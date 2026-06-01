SELECT 
	 T0.DocEntry
	,T0.DocNum
	,T0.DocDate
	,T0.DocDueDate
	,T0.CardCode
	,C0.CardName
	,P0.Name [SupplierContactPerson]
	,ISNULL(T0.Comments, '') [Remarks]
	,CASE 
		WHEN T0.CANCELED = 'Y' THEN 'Cancelled'	
		WHEN T0.DocStatus = 'C' THEN 'Closed'
	ELSE 'Open'
	End as DocStatus
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
	,T0.U_RevBy [ReviewedBy]
	,T0.U_PurchType [PurchaseType]
	,T0.U_ItemName [ItemName]
	,T0.U_Remarks [DocRemarks]
	,AX1.*
FROM OPOR AS T0
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
INNER JOIN OCRD AS C0 ON T0.CardCode = C0.CardCode
LEFT JOIN OCPR AS P0 ON T0.CntctCode = P0.CntctCode
WHERE 
	T0.DocStatus = 'O'
	AND T0.DocEntry = @DocEntry