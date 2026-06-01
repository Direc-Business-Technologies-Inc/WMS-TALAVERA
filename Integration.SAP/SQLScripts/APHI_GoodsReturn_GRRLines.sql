SELECT
	 T0.DocEntry
	,T0.DocNum
	,T1.LineNum
	,T1.WhsCode
	,T8.WhsName
	,T0.CardCode
	,T0.CardName
	,T1.ItemCode
	,T6.ItemName
	,T1.Quantity [TargetQty]
	,T1.OpenQty
	,ISNULL(T1.unitMsr, 'Manual') [UoMCode]
	,T1.NumPerMsr [UoMValue]
	,T7.UomName [UoMName]
	,CASE 
		WHEN T0.CANCELED = 'Y' THEN 'Cancelled'	
		WHEN T0.DocStatus = 'C' THEN 'Closed'
	 ELSE 'Open'
	 END as DocStatus
FROM OPRR T0
INNER JOIN PRR1 T1 ON T1.DocEntry = T0.DocEntry
INNER JOIN OITM T6 ON T6.ItemCode = T1.ItemCode
LEFT JOIN OUOM AS T7 ON T7.UomEntry = T1.UomEntry
INNER JOIN OWHS T8 ON T8.WhsCode = T1.WhsCode
WHERE 
	T0.DocStatus = 'O'
	AND T0.DocEntry = @DocEntry
ORDER BY 
	 T1.OpenQty
	,T1.LineNum