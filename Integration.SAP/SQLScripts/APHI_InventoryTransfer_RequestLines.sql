SELECT T0.ItemCode
    ,T0.LineNum
	,T0.Dscription [ItemName]
	,T0.unitMsr [UoMName]
	,T0.U_SBAPlan [AllotedQuantity]
	,T0.Quantity
	,T0.OpenQty [OpenQuantity]
    ,ISNULL(
        (SELECT SUM(_T2.Quantity)
         FROM ODRF _T0
         LEFT JOIN DRF1 _T2 ON _T0.DocEntry = _T2.DocEntry
         WHERE _T0.ObjType = 67 
           AND _T2.BaseEntry = T0.DocEntry
           AND _T0.DocStatus = 'O' -- Open Drafts only
           AND _T2.ItemCode = T0.ItemCode  -- Ensure matching item
        ), 0
    ) AS "PendingQuantity"
FROM WTQ1 AS T0
WHERE T0.DocEntry = @DocEntry