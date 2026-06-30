SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	t.subsidiary as NetsuiteSubsidiaryInternalId,
	t.memo as Memo,
	
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM
    transaction t
WHERE
    t.type = 'ItemShip' AND
	t.status = 'B'
