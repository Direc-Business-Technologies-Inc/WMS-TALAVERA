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
    AND t.status IN ('F', 'E') 
	AND BUILTIN.DF(t.custbody_dbti_purchase_category) = 'Trade'
	AND (
        (t.recordtype = 'intercompanytransferorder' AND t.tosubsidiary = @subsidiaryid)
        OR
        (t.recordtype = 'transferorder' AND t.subsidiary = @subsidiaryid)
    )