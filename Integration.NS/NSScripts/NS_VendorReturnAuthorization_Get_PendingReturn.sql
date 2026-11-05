SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	e.id as CustomerEntityId,
	e.fullname as CustomerName,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
	
FROM
	transaction t
JOIN
	entity e ON t.entity = e.id
JOIN
	(SELECT opk.transaction AS packtran
	 FROM transactionline opk
	 WHERE opk.mainline = 'F'
	   AND opk.isclosed = 'F'
	   AND ABS(opk.quantity) - ABS(opk.quantityshiprecv) > 0
	 GROUP BY opk.transaction) openpk ON openpk.packtran = t.id
WHERE
	t.recordtype = 'vendorreturnauthorization'
	AND t.status IN ('B', 'E') AND
	t.subsidiary = @subsidiaryid