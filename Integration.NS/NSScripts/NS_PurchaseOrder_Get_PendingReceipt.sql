SELECT
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
	entity e ON t.entity = e.id
WHERE 
	t.recordtype = 'purchaseorder' AND
	t.status IN ('B', 'E')