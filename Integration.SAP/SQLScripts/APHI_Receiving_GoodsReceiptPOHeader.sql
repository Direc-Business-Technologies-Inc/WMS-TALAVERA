SELECT 
	 T0.DocEntry
	,T0.DocNum
	,ISNULL(T3.DocEntry, -1) [BaseEntry]
	,ISNULL(T3.DocNum, -1) [BaseDocNum]
	,T0.DocDate
	,T0.DocDueDate
	,T0.CardCode
	,T5.CardName
	,T6.Name [SupplierContactPerson]
	,T0.U_PrepBy [PreparedBy]
	,T0.U_PONo [PONo]
	,T0.U_DRNo [DRNo]
	,T0.U_Desig [Designation]
	,T0.U_RecBy [ReceivedBy]
	,T0.U_AppBy [ApprovedBy]
	,T0.U_NotedBy [NotedBy]
	,T0.U_SchlYear [SchoolYear]
	,T0.U_SINo [SINo]
	,T0.U_DelBy [DeliveredBy]
	,T0.U_Time [Time]
	,T0.U_RevBy [ReviewedBy]
	,T0.U_PurchType [PurchaseType]
	,T0.U_ItemName [ItemName]
	,T0.U_Remarks [DocRemarks]
	,AX1.*
FROM OPDN AS T0
OUTER APPLY (
	SELECT
		 STRING_AGG(ItmsGrpCod, ', ')    [ItemGroupCodes]
	FROM (
		SELECT DISTINCT
			 C1.ItmsGrpCod
			,C2.ItmsGrpNam
		FROM POR1 T1
		INNER JOIN OITM AS C1 ON T1.ItemCode = C1.ItemCode
		INNER JOIN OITB AS C2 ON C1.ItmsGrpCod = C2.ItmsGrpCod
		WHERE T1.DocEntry = T0.DocEntry
	) Distinct_Groups
) AX1
INNER JOIN PDN1 AS T1 ON T0.DocEntry = T1.DocEntry
INNER JOIN OPOR AS T3 ON T1.BaseEntry = T3.DocEntry
INNER JOIN OCRD AS T5 ON T0.CardCode = T5.CardCode
LEFT JOIN OCPR AS T6 ON T3.CntctCode = T6.CntctCode
WHERE 
	T0.Comments LIKE '%WMS%'
	AND T0.DocEntry = @DocEntry