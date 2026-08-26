SELECT DISTINCT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
	
FROM
	transaction t
JOIN 
	transactionline tl ON t.id = tl.transaction
JOIN 
	employee emp  ON emp.id = @userid AND UPPER(TRIM(BUILTIN.DF(emp.custentity_dbti_wms_user_location_access))) LIKE '%' || UPPER(TRIM(BUILTIN.DF(tl.location))) || '%'
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
ORDER BY t.lastmodifieddate desc