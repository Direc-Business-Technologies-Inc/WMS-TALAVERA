SELECT
	 T0.DocEntry
	,T0.DocNum
	,T0.DocDate
	,T0.DocDueDate
	,T0.CardCode
	,T0.CardName
	,AX1.*	
	,CASE 
		WHEN T0.CANCELED = 'Y' THEN 'Cancelled'	
		WHEN T0.DocStatus = 'C' THEN 'Closed'
	 ELSE 'Open'
	 END as DocStatus
FROM ORPD T0
OUTER APPLY (
	SELECT TOP 1
		 ISNULL(T7.DocEntry, -1) [GRRDocEntry]
		,ISNULL(T7.DocNum, -1) [GRRDocNum]
		,ISNULL(T3.DocEntry, -1) [GRPODocEntry]
		,ISNULL(T3.DocNum, -1) [GRPODocNum]
		,ISNULL(T5.DocEntry, -1) [PODocEntry]
		,ISNULL(T5.DocNum, -1) [PODocNum]
		,ISNULL(T0.Comments, '') [Remarks]
	FROM RPD1 T1  
	LEFT JOIN PDN1 T2 ON T2.DocEntry = T1.BaseEntry AND T2.ObjType = T1.BaseType
	LEFT JOIN OPDN T3 ON T3.DocEntry = T2.DocEntry
	LEFT JOIN  POR1 T4 ON T4.DocEntry = T3.BaseEntry AND T4.ObjType = T2.BaseType
	LEFT JOIN OPOR T5 ON T5.DocEntry = T4.DocEntry
	LEFT JOIN  RRR1 T6 ON T6.DocEntry = T1.BaseEntry AND T6.ObjType = T1.BaseType
	LEFT JOIN ORRR T7 ON T7.DocEntry = T6.DocEntry
	WHERE T1.DocEntry = T0.DocEntry
) AX1
WHERE T0.Comments LIKE '%WMS%'