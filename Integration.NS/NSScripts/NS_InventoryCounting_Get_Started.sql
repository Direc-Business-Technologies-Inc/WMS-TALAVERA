SELECT
    ic.id AS NetsuiteICInternalId,
    ic.tranid AS InventoryCountingNumber,
    ic.status AS Status,
    ic.trandate AS NetsuiteInventoryCountingDate,
FROM inventorycount ic
WHERE ic.status = 'B'
ORDER BY ic.id