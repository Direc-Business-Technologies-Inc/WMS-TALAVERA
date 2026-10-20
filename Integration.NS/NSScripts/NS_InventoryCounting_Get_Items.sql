SELECT
    t.id AS NetsuiteOrderInternalId,
    t.tranid AS OrderNumber,
    t.recordtype AS OrderType,
    t.status AS OrderStatus,

    t.subsidiary AS NetsuiteSubsidiaryInternalId,

    tl.id AS LineSequenceNumber,
    tl.transactionlinetype AS TransactionLineType,

    tl.item AS NetsuiteMaterialInternalId,
    i.itemid AS MaterialCode,
    i.displayname AS MaterialName,
    i.weight AS MaterialWeight,

    tl.quantity AS LineQuantity,
    uom.unitname AS UoMName,
    uom.conversionrate AS UoMRate,

    tl.countquantity AS CountedQuantity,

    COALESCE((SELECT SUM(ia.quantity) FROM inventoryassignment ia
              WHERE ia.transaction = t.id AND ia.transactionline = tl.id
                AND ia.inventorystatus = 1), 0) AS GoodCountedQuantity,

    COALESCE((SELECT SUM(ia.quantity) FROM inventoryassignment ia
              WHERE ia.transaction = t.id AND ia.transactionline = tl.id
                AND ia.inventorystatus = 3), 0) AS BadCountedQuantity,

    (SELECT MAX(ia.id) FROM inventoryassignment ia
     WHERE ia.transaction = t.id AND ia.transactionline = tl.id
       AND ia.inventorystatus = 1) AS NetsuiteInventoryDetailInternalId,

    (SELECT MAX(ia.id) FROM inventoryassignment ia
     WHERE ia.transaction = t.id AND ia.transactionline = tl.id
       AND ia.inventorystatus = 3) AS BadInventoryDetailId,

    TO_CHAR(t.createddate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate

FROM item i
JOIN transactionline tl ON i.id = tl.item
JOIN transaction t ON tl.transaction = t.id
JOIN unitstypeuom uom ON tl.units = uom.internalid
WHERE
    t.status = 'B'
    AND t.recordType = 'inventorycount'
    AND tl.transactionlinetype = 'COUNTQUANTITY'
    AND t.tranid = @tranid
