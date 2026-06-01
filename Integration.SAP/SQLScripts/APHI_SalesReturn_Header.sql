SELECT
	 ORDN.DocEntry
	,ORDN.DocNum
	,ORDN.DocDate
	,ORDN.DocDueDate
	,ORDN.CardCode
	,ORDN.CardName
	,OCRD.CntctPrsn [ContactPerson]
	,ORDN.NumAtCard
	,ORDN.U_SchlYear [SchoolYear]
	,ORDN.U_RetType [ReturnType]
	,ORDN.U_PURNo [PURNo]
	,ORDN.U_DRNo [DRNo]
	,ORDN.U_SONo [SONo]
	,ORDN.U_SINo [SINo]
	,ORDN.U_Desig [Designation]
	,ORDN.U_Remarks [DocRemarksa]
	,ORDN.U_RetBy [ReturnedBy]
	,ORDN.U_PickBy [PickBy]
	,ORDN.U_PrepBy [PreparedBy]
	,ORDN.U_CheckBy [CheckedBy]
	,ORDN.U_NotedBy [NotedBy]
	,ORDN.U_AppBy [ApprovedBy]
FROM ORDN
INNER JOIN OCRD ON ORDN.CardCode = OCRD.CardCode
WHERE ORDN.DocEntry = @DocEntry