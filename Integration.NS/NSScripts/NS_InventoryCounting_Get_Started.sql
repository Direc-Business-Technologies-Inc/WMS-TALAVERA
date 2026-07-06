SELECT
    t.id AS NetsuiteOrderInternalId,
    t.tranid AS OrderNumber,
    t.recordtype AS OrderType,
    t.status AS OrderStatus,

    TO_CHAR(t.createddate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM transaction t

WHERE
    t.status = 'B'
    AND t.recordType = 'inventorycount' AND
	t.subsidiary = @subsidiaryid