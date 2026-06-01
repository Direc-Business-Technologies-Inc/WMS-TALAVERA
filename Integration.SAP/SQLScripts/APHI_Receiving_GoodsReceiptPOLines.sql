SELECT 
	 T0.DocEntry
	,T0.DocNum
	,T1.LineNum
	,T3.WhsCode
	,T3.WhsName
	,T1.ItemCode
	,T2.ItemName
	,REPLACE(ISNULL(T2.U_ISBN, ''), '-', '') [ISBN]
	,T1.OpenQty
	,T1.Quantity
	,ISNULL(T1.unitMsr, 'Manual') [UoMCode]
	,T1.NumPerMsr [UoMValue]
	,T4.UomName [UoMName]
	,T1.U_InputType [InputType]
FROM OPDN AS T0
INNER JOIN PDN1 AS T1 ON T0.DocEntry = T1.DocEntry
INNER JOIN OITM AS T2 ON T1.ItemCode = T2.ItemCode
INNER JOIN OWHS AS T3 ON T1.WhsCode = T3.WhsCode
LEFT JOIN OUOM AS T4 ON T1.UomEntry = T4.UomEntry
WHERE
	T0.DocEntry = @DocEntry
ORDER BY
	T1.LineNum