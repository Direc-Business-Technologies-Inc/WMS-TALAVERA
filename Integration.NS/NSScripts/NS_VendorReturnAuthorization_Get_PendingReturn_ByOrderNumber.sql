SELECT t.id AS NetsuiteOrderInternalId, t.tranId AS OrderNumber, t.recordtype AS OrderType,
	t.status AS OrderStatus, e.id AS CustomerEntityId, e.fullname AS CustomerName,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM transaction t
JOIN entity e ON t.entity = e.id
JOIN employee emp ON emp.id = @userid
JOIN transactionline tl ON tl.transaction = t.id
JOIN (SELECT opk.transaction AS packtran FROM transactionline opk
	WHERE opk.mainline = 'F' AND opk.isclosed = 'F'
	AND ABS(opk.quantity) - ABS(opk.quantityshiprecv) > 0 GROUP BY opk.transaction) openpk
	ON openpk.packtran = t.id
WHERE t.recordtype = 'vendorreturnauthorization' AND t.status IN ('B', 'E')
	AND t.subsidiary = @subsidiaryid AND t.tranid = @ordernumber
	AND UPPER(TRIM(BUILTIN.DF(emp.custentity_dbti_wms_user_location_access))) LIKE '%' || UPPER(TRIM(BUILTIN.DF(tl.location))) || '%'
