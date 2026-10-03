SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	lsm.subsidiary as NetsuiteToSubsidiaryInternalId,
	t.transferlocation as NetsuiteToLocationInternalId,
	t.memo as Memo,
	NVL(t.custbody_dbti_fully_received, 'F') AS IsReceived,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM
    transaction t
JOIN locationSubsidiaryMap lsm
    ON t.transferlocation = lsm.location
WHERE
    t.type = 'ItemShip' AND
	(t.status = 'B' OR t.custbody_dbti_print_label = 'T') AND
	(t.custbody_dbti_fully_received = 'F' OR t.custbody_dbti_fully_received IS NULL)
