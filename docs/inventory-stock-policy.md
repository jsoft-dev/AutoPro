# Inventory stock policy (GRN / PO)

This documents how stock increases are recorded in AutoPro after GRN and purchase orders were added.

## Stock increase (recommended)

- **Goods Receipt Note (GRN)** — primary path when stock arrives from a supplier. Draft GRNs do not change balances; **Posted** GRNs increase `QuantityOnHand` per line.
- **Purchase order (optional)** — order from supplier; no stock change until a GRN is posted. GRN may reference a PO and update received quantities on PO lines.

## Stock increase (allowed exceptions)

- **Admin part form** — direct edit of on-hand quantity for corrections (cycle count, write-on, data fix). These changes are not tied to a GRN; use sparingly.
- **Excel import** — bulk load or periodic sync; sets absolute quantities and can create suppliers. Coexists with GRN; prefer GRN for day-to-day receiving.

## Stock decrease

- Job or counter-sale **shop purchase** lines (inventory source).
- **Reset workflow** on a job restores quantities from removed inventory purchase lines only.

## Pricing on receipt

- Posted GRN lines may optionally update the part **sell price** (`UnitPrice`) from line unit cost when `updateSellingPriceFromUnitCost` is set on post.

## Single location

- All stock is implicit shop stock; warehouses are out of scope.