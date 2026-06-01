SELECT
	ODLN.DocEntry
	,ODLN.DocNum
	,ODLN.DocDate
	,ODLN.DocDueDate
	,ODLN.CardCode
	,ODLN.CardName
	,OCRD.CntctPrsn [ContactPerson]
	,ODLN.NumAtCard
	,ODLN.U_SchlYear [SchoolYear]
	,ODLN.U_DRNo [DRNo]
	,ODLN.U_ActualDelivDate [ActualDeliveryDate]
	,ODLN.U_DelivMeans [DeliveryMeans]
	,ODLN.U_Courier [Courier]
	,ODLN.U_CourName [CourierName]
	,ODLN.U_Desig [Designation]
	,ODLN.U_WBNo [WaybillNo]
	,ODLN.U_PlateNo [PlateNo]
	,ODLN.U_Driver [Driver]
	,ODLN.U_Remarks [DocRemarks]
	,ODLN.U_RecBy [ReceivedBy]
	,ODLN.U_PrepBy [PreparedBy]
	,ODLN.U_AppBy [ApprovedBy]
	,ODLN.U_NotedBy [NotedBy]
FROM ODLN
INNER JOIN OCRD ON ODLN.CardCode = OCRD.CardCode
WHERE ODLN.DocEntry = @DocEntry