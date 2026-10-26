SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranId as OrderNumber,
	t.recordtype as OrderType,
	t.status as OrderStatus,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
	
FROM
	transaction t
JOIN
	employee emp ON emp.id = @userid
WHERE
    t.recordtype IN ('intercompanytransferorder', 'transferorder')
	AND t.custbody_dbti_transfer_category IN ('1', '2')
    AND t.status IN ('F', 'E') 
	AND BUILTIN.DF(t.custbody_dbti_purchase_category) = 'Trade'
	AND EXISTS (
		SELECT 1
		FROM MAP_employee_custentity_dbti_wms_user_location_access map
		JOIN transactionline tl ON tl.location = map.maptwo
		WHERE tl.transaction = t.id
			AND map.mapone = emp.id
	)

	AND (
        (t.recordtype = 'intercompanytransferorder' AND t.tosubsidiary = @subsidiaryid)
        OR
        (t.recordtype = 'transferorder' AND t.subsidiary = @subsidiaryid)
    )
ORDER BY t.lastmodifieddate desc
