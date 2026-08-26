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
    t.recordtype IN ('intercompanytransferorder', 'transferorder')
	AND t.custbody_dbti_transfer_category IN ('1', '2')
	AND t.ordpicked = 'F'
    AND t.status IN ('B', 'D', 'E')
	AND BUILTIN.DF(t.custbody_dbti_purchase_category) = 'Trade'
	AND (
        (t.recordtype = 'intercompanytransferorder' AND t.subsidiary = @subsidiaryid)
        OR
        (t.recordtype = 'transferorder' AND t.subsidiary = @subsidiaryid)
    )
ORDER BY t.lastmodifieddate desc	