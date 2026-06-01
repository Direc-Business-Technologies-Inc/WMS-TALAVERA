SELECT 
	 T0.DocEntry
	,T0.DocNum
	,T1.LineNum
	,T1.ItemCode
	,T1.Price
	,T1.Quantity [TargetQuantity]
	,T1.OpenQty [OpenQuantity]
	,ISNULL(T1.unitMsr, 'Manual') [UoMCode]
	,T1.NumPerMsr [UoMValue]
	,T4.UomName [UoMName]
	,T2.ItemName
	,REPLACE(ISNULL(T2.U_ISBN, ''), '-', '') [ISBN]
	,T1.WhsCode
	,T3.WhsName
	,T1.VatGroup
FROM OPOR AS T0
INNER JOIN POR1 AS T1 ON T0.DocEntry = T1.DocEntry
INNER JOIN OITM AS T2 ON T1.ItemCode = T2.ItemCode
INNER JOIN OWHS AS T3 ON T1.WhsCode = T3.WhsCode
LEFT JOIN OUOM AS T4 ON T1.UomEntry = T4.UomEntry
WHERE 
	T0.DocStatus = 'O'
	AND T0.DocEntry = @DocEntry
ORDER BY
	 T1.OpenQty desc
	,T1.LineNum