SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
	
FROM
	transaction t

WHERE
    t.recordtype IN ('intercompanytransferorder', 'transferorder')
	AND t.custbody_dbti_transfer_category IN ('1', '2')
    AND t.status IN ('F', 'E') AND
	t.subsidiary = @subsidiaryid