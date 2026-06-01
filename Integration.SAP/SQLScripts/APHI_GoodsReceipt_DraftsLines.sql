SELECT 
	 T0.DocEntry
	,T0.DocNum
	,T1.LineNum
	,T1.ItemCode
	,T2.ItemName
	,T1.Quantity
	,T1.WhsCode
	,T3.WhsName
	,ISNULL(T1.unitMsr, 'Manual') [UoMCode]
	,T1.NumPerMsr [UoMValue]
	,T4.UomName [UoMName]
FROM ODRF T0
INNER JOIN DRF1 T1 ON T1.DocEntry = T0.DocEntry
INNER JOIN OITM T2 ON T1.ItemCode = T2.ItemCode
LEFT JOIN OWHS T3 ON T1.WhsCode = T3.WhsCode
LEFT JOIN OUOM AS T4 ON T4.UomEntry = T1.UomEntry
WHERE 
	T0.ObjType = @ObjType 
	AND T0.DocEntry = @DocEntry
