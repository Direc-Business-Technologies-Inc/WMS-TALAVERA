SELECT DISTINCT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	e.id as CustomerEntityId,
	e.fullname as CustomerName,
	TO_CHAR(t.custbody_dbti_est_receipt_date, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderDocumentDate,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
	
FROM
	transaction t
JOIN 
	transactionline tl ON t.id = tl.transaction
JOIN 
	entity e ON t.entity = e.id
JOIN 
	employee emp  ON emp.id = @userid AND UPPER(TRIM(BUILTIN.DF(emp.custentity_dbti_wms_user_location_access))) LIKE '%' || UPPER(TRIM(BUILTIN.DF(tl.location))) || '%'
WHERE 
	t.recordtype = 'purchaseorder' AND
	t.status IN ('B', 'E') AND
	t.subsidiary = @subsidiaryid