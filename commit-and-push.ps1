cd "c:\Users\mosta\Downloads\projects\Market-Api"
git config user.email m.ssaid356@gmail.com
git config user.name "Mostafa SAID"

Write-Output "=== Git Status ==="
git status

Write-Output ""
Write-Output "=== Staging Changes ==="
git add -A
git status

Write-Output ""
Write-Output "=== Committing ==="
git commit -m "fix(orders): calculate order pricing server-side

- Remove client-controllable financial fields from CreateOrderCommand
  (SubTotal, ShippingCost, Tax)
- Remove client-supplied fields from OrderItemInput
  (ProductName, VendorId, Price)
- Implement server-side pricing calculation in handler:
  * Unit price resolved from Product record (with discount)
  * Tax calculated as 8% of subtotal
  * Shipping cost set as flat 5.00 rate
- Update tests to verify server-side pricing
- Add test to verify client fields do not exist on DTOs
- All financial values now come from authoritative server data"

Write-Output ""
Write-Output "=== Pushing to origin/fix/order-server-side-pricing ==="
git push -u origin fix/order-server-side-pricing

Write-Output ""
Write-Output "=== Done ==="
