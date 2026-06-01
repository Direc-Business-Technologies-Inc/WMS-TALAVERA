SELECT
	 T0.DocEntry
	,T0.DocNum
	,T0.DocDate
	,T0.DocDueDate
	,T0.CardCode
	,T0.CardName
	,ISNULL(T7.DocEntry, -1) [GRRDocEntry]
	,ISNULL(T7.DocNum, -1) [GRRDocNum]
	,ISNULL(T3.DocEntry, -1) [GRPODocEntry]
	,ISNULL(T3.DocNum, -1) [GRPODocNum]
	,ISNULL(T5.DocEntry, -1) [PODocEntry]
	,ISNULL(T5.DocNum, -1) [PODocNum]
	,ISNULL(T0.Comments, '') [Remarks]
	,CASE 
		WHEN T0.CANCELED = 'Y' THEN 'Cancelled'	
		WHEN T0.DocStatus = 'C' THEN 'Closed'
	 ELSE 'Open'
	 END as DocStatus
	,T0.U_SchlYear [SchoolYear]
	,T0.U_RetType [ReturnType]
	,T0.U_DRNo [DRNo]
	,T0.U_SINo [SINo]
	,T0.U_DelBy [DeliveredBy]
	,T0.U_RecBy [ReceivedBy]
	,T0.U_Remarks [DocRemarks]
	,T0.U_PrepBy [PreparedBy]
	,T0.U_RevBy [ReviewedBy]
	,T0.U_AppBy [ApprovedBy]
	,T0.U_CheckBy [CheckedBy]
	,AX1.WhsCode
	,T8.WhsName
FROM ORPD T0
OUTER APPLY (
	SELECT
		X1.WhsCode
	FROM RPD1 X1
	WHERE X1.DocEntry = T0.DocEntry
) AX1
INNER JOIN RPD1 T1 ON T1.DocEntry = T0.DocEntry
LEFT JOIN PDN1 T2 ON T2.DocEntry = T1.BaseEntry AND T2.ObjType = T1.BaseType
LEFT JOIN OPDN T3 ON T3.DocEntry = T2.DocEntry
LEFT JOIN  POR1 T4 ON T4.DocEntry = T3.BaseEntry AND T4.ObjType = T2.BaseType
LEFT JOIN OPOR T5 ON T5.DocEntry = T4.DocEntry
LEFT JOIN  RRR1 T6 ON T6.DocEntry = T1.BaseEntry AND T6.ObjType = T1.BaseType
LEFT JOIN ORRR T7 ON T7.DocEntry = T6.DocEntry
INNER JOIN OWHS T8 ON AX1.WhsCode = T8.WhsCode
WHERE T0.DocEntry = @DocEntry