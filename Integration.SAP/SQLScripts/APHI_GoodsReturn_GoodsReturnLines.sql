SELECT
	 T0.DocEntry
	,T0.DocNum
	,T1.WhsCode
	,T8.WhsName
	,T0.CardCode
	,T0.CardName
	,T1.ItemCode
	,T6.ItemName
	,T1.Quantity
	,ISNULL(T1.unitMsr, 'Manual') [UoMCode]
	,T1.NumPerMsr [UoMValue]
	,T7.UomName [UoMName]
	,CASE 
		WHEN T0.CANCELED = 'Y' THEN 'Cancelled'	
		WHEN T0.DocStatus = 'C' THEN 'Closed'
	 ELSE 'Open'
	 END as DocStatus
FROM ORPD T0
INNER JOIN RPD1 T1 ON T1.DocEntry = T0.DocEntry
LEFT JOIN PDN1 T2 ON T2.DocEntry = T1.BaseEntry AND T2.ObjType = T1.BaseType
LEFT JOIN OPDN T3 ON T3.DocEntry = T2.DocEntry
LEFT JOIN  POR1 T4 ON T4.DocEntry = T3.BaseEntry AND T4.ObjType = T2.BaseType
LEFT JOIN OPOR T5 ON T5.DocEntry = T4.DocEntry
INNER JOIN OITM T6 ON T6.ItemCode = T1.ItemCode
LEFT JOIN OUOM AS T7 ON T7.UomEntry = T1.UomEntry
INNER JOIN OWHS T8 ON T8.WhsCode = T1.WhsCode
WHERE T0.DocEntry = @DocEntry
ORDER BY T1.LineNum