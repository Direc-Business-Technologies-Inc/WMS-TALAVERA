SELECT
	 ORRR.DocEntry
	,ORRR.DocNum
	,ORRR.DocDate
	,ORRR.DocDueDate
	,ORRR.CardCode
	,ORRR.CardName
	,OCRD.CntctPrsn [ContactPerson]
	,ORRR.NumAtCard
	,ORRR.U_SchlYear [SchoolYear]
	,ORDN.U_RetType [ReturnType]
	,ORRR.U_PURNo [PURNo]
	,ORRR.U_DRNo [DRNo]
	,ORRR.U_SONo [SONo]
	,ORRR.U_SINo [SINo]
	,ORRR.U_Desig [Designation]
	,ORRR.U_Remarks [DocRemarksa]
	,ORRR.U_RetBy [ReturnedBy]
	,ORRR.U_PickBy [PickBy]
	,ORRR.U_PrepBy [PreparedBy]
	,ORRR.U_CheckBy [CheckedBy]
	,ORRR.U_NotedBy [NotedBy]
	,ORRR.U_AppBy [ApprovedBy]
FROM ORRR
INNER JOIN OCRD ON ORRR.CardCode = OCRD.CardCode
WHERE ORRR.DocEntry = @DocEntry