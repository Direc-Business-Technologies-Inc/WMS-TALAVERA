SELECT t.id AS NetsuiteOrderInternalId, t.tranId AS OrderNumber, t.recordtype AS OrderType,
	t.status AS OrderStatus, t.custbody_dbti_transfer_category AS TransferCategory,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM transaction t
JOIN employee emp ON emp.id = @userid
JOIN (SELECT opk.transaction AS packtran FROM transactionline opk
	WHERE opk.mainline = 'F' AND opk.isclosed = 'F'
	AND ABS(opk.quantity) - ABS(opk.quantitypacked) > 0 GROUP BY opk.transaction) openpk
	ON openpk.packtran = t.id
WHERE t.recordtype IN ('intercompanytransferorder', 'transferorder')
	AND t.custbody_dbti_transfer_category IN ('1', '2')
	AND t.ordpicked = 'F' AND t.status IN ('B', 'D', 'E')
	AND BUILTIN.DF(t.custbody_dbti_purchase_category) = 'Trade'
	AND t.subsidiary = @subsidiaryid
	AND t.tranid = @ordernumber
