SELECT
	 T0.DocEntry
	,T0.DocNum
	,T1.LineNum
	,T1.WhsCode
	,T8.WhsName
	,T1.ItemCode
	,T2.ItemName
	,T1.Quantity
	,ISNULL(T1.unitMsr, 'Manual') [UoMCode]
	,T1.NumPerMsr [UoMValue]
	,T3.UomName [UoMName]
FROM OIGN T0
INNER JOIN IGN1 T1 ON T1.DocEntry = T0.DocEntry
INNER JOIN OITM T2 ON T2.ItemCode = T1.ItemCode
LEFT JOIN OUOM AS T3 ON T3.UomEntry = T1.UomEntry
INNER JOIN OWHS T8 ON T8.WhsCode = T1.WhsCode
WHERE T0.DocEntry = @DocEntry