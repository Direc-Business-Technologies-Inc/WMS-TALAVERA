SELECT DISTINCT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	t.custbody_dbti_transfer_category as TransferCategory,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
	
FROM
	transaction t
JOIN 
	transactionline tl ON t.id = tl.transaction
JOIN 
	employee emp  ON emp.id = @userid AND UPPER(TRIM(BUILTIN.DF(emp.custentity_dbti_wms_user_location_access))) LIKE '%' || UPPER(TRIM(BUILTIN.DF(tl.location))) || '%'
WHERE
    t.recordtype = 'intercompanytransferorder'
	AND t.custbody_dbti_transfer_category IN ('3', '4')
    AND t.status IN ('F', 'E') AND
	t.tosubsidiary = @subsidiaryid
	