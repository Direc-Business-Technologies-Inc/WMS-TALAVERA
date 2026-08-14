SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	t.custbody_dbti_transfer_category as TransferCategory,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
	
FROM
	transaction t

WHERE
    t.recordtype IN ('intercompanytransferorder')
	AND t.custbody_dbti_transfer_category IN ('3', '4')
	AND t.ordpicked = 'F'
    AND t.status IN ('B', 'D', 'E') AND
	t.subsidiary = @subsidiaryid
	