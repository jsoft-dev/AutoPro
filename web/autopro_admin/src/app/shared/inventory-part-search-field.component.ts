import { CurrencyPipe } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  inject,
  model,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  firstValueFrom,
  map,
  of,
  Subject,
  switchMap,
} from 'rxjs';

import { InventorySearchItem } from '../models/inventory.models';
import { InventoryService } from '../services/inventory.service';

const PAGE_SIZE = 20;
const SCROLL_LOAD_THRESHOLD_PX = 72;

@Component({
  selector: 'app-inventory-part-search-field',
  standalone: true,
  imports: [FormsModule, CurrencyPipe],
  templateUrl: './inventory-part-search-field.component.html',
  styleUrl: './inventory-part-search-field.component.scss',
})
export class InventoryPartSearchFieldComponent {
  private readonly inventoryApi = inject(InventoryService);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly value = model<InventorySearchItem | null>(null);

  readonly open = signal(false);
  readonly panelQuery = signal('');
  readonly items = signal<InventorySearchItem[]>([]);
  readonly hasMore = signal(false);
  readonly loading = signal(false);
  readonly loadingMore = signal(false);
  readonly listError = signal<string | null>(null);

  private readonly query$ = new Subject<string>();
  private loadGeneration = 0;
  private activeQuery = '';

  readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  constructor() {
    this.query$
      .pipe(
        debounceTime(280),
        distinctUntilChanged(),
        switchMap((query) => {
          const generation = ++this.loadGeneration;
          this.activeQuery = query.trim();
          this.loading.set(true);
          this.loadingMore.set(false);
          this.listError.set(null);
          this.items.set([]);
          this.hasMore.set(false);
          return this.inventoryApi.searchForJob(this.activeQuery, PAGE_SIZE, 0).pipe(
            map((page) => ({ page, generation, failed: false as const })),
            catchError((err) =>
              of({
                page: { items: [] as InventorySearchItem[], hasMore: false },
                generation,
                failed: true as const,
                message: (err?.error?.message as string) ?? 'Could not load parts.',
              }),
            ),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        if (result.generation !== this.loadGeneration) return;
        if (result.failed) {
          this.loading.set(false);
          this.listError.set(result.message ?? 'Could not load parts.');
          return;
        }
        this.items.set(result.page.items);
        this.hasMore.set(result.page.hasMore);
        this.loading.set(false);
      });
  }

  partLabel(part: InventorySearchItem): string {
    return `${part.partNumber} — ${part.name}`;
  }

  triggerLabel(): string {
    const selected = this.value();
    return selected ? this.partLabel(selected) : 'Search by part number or name';
  }

  togglePanel(event: MouseEvent): void {
    event.stopPropagation();
    if (this.open()) {
      this.closePanel();
      return;
    }
    this.openPanel();
  }

  openPanel(): void {
    this.open.set(true);
    this.panelQuery.set('');
    this.query$.next('');
    queueMicrotask(() => this.searchInput()?.nativeElement.focus());
  }

  closePanel(): void {
    this.open.set(false);
  }

  onPanelQueryChange(value: string): void {
    this.panelQuery.set(value);
    this.query$.next(value);
  }

  clearSelection(event: MouseEvent): void {
    event.stopPropagation();
    this.value.set(null);
    this.closePanel();
  }

  selectPart(part: InventorySearchItem): void {
    this.value.set(part);
    this.closePanel();
  }

  onListScroll(event: Event): void {
    const el = event.target as HTMLElement;
    if (!this.hasMore() || this.loadingMore() || this.loading()) return;
    if (el.scrollHeight - el.scrollTop - el.clientHeight > SCROLL_LOAD_THRESHOLD_PX) return;
    void this.loadNextPage();
  }

  private async loadNextPage(): Promise<void> {
    if (!this.hasMore() || this.loadingMore() || this.loading()) return;

    const generation = this.loadGeneration;
    const offset = this.items().length;
    this.loadingMore.set(true);

    try {
      const page = await firstValueFrom(
        this.inventoryApi.searchForJob(this.activeQuery, PAGE_SIZE, offset),
      );
      if (generation !== this.loadGeneration) {
        this.loadingMore.set(false);
        return;
      }
      this.items.update((rows) => [...rows, ...page.items]);
      this.hasMore.set(page.hasMore);
      this.loadingMore.set(false);
    } catch {
      if (generation === this.loadGeneration) {
        this.loadingMore.set(false);
        this.listError.set('Could not load more parts.');
      }
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;
    if (this.host.nativeElement.contains(event.target as Node)) return;
    this.closePanel();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closePanel();
  }
}
