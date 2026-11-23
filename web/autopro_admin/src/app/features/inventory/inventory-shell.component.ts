import { Component } from '@angular/core';
import { IsActiveMatchOptions, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-inventory-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './inventory-shell.component.html',
  styleUrl: './inventory-shell.component.scss',
})
export class InventoryShellComponent {
  readonly tabActiveOptions: IsActiveMatchOptions = {
    paths: 'subset',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  };
}