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

    COALESCE(ig.GoodCountedQuantity, 0) AS GoodCountedQuantity,
    COALESCE(ib.BadCountedQuantity, 0) AS BadCountedQuantity,
    ig.GoodInventoryDetailId AS NetsuiteInventoryDetailInternalId,
    ib.BadInventoryDetailId AS BadInventoryDetailId,

    TO_CHAR(t.createddate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate

FROM item i
JOIN transactionline tl ON i.id = tl.item
JOIN transaction t ON tl.transaction = t.id
JOIN unitstypeuom uom ON tl.units = uom.internalid
LEFT JOIN (
    SELECT ia.transaction AS TransactionId,
           ia.transactionline AS TransactionLineId,
           SUM(ia.quantity) AS GoodCountedQuantity,
           MAX(ia.id) AS GoodInventoryDetailId
    FROM inventoryassignment ia
    WHERE ia.inventorystatus = '1'
    GROUP BY ia.transaction, ia.transactionline
) ig ON ig.TransactionId = t.id AND ig.TransactionLineId = tl.id
LEFT JOIN (
    SELECT ia.transaction AS TransactionId,
           ia.transactionline AS TransactionLineId,
           SUM(ia.quantity) AS BadCountedQuantity,
           MAX(ia.id) AS BadInventoryDetailId
    FROM inventoryassignment ia
    WHERE ia.inventorystatus = '3'
    GROUP BY ia.transaction, ia.transactionline
) ib ON ib.TransactionId = t.id AND ib.TransactionLineId = tl.id
WHERE
    t.status = 'B'
    AND t.recordType = 'inventorycount'
    AND tl.transactionlinetype = 'COUNTQUANTITY'
    AND t.tranid = @tranid
