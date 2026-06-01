SELECT
	 ORDR.DocEntry
	,ORDR.DocNum
	,ORDR.DocDate
	,ORDR.DocDueDate
	,ORDR.DocStatus
	,OCRD.CardCode
	,OCRD.CardName
	,OCRD.CntctPrsn [ContactPerson]
	,ORDR.NumAtCard
	,ORDR.U_SchlYear [SchoolYear]
	,ORDR.U_PONo [PONo]
	,ORDR.U_Area [Area]
	,ORDR.U_Desig [Designation]
	,ORDR.U_OrdBy [OrderBy]
	,ORDR.U_Remarks [DocRemarks]
	,ORDR.U_PrepBy [PreparedBy]
	,ORDR.U_RevBy [ReviewedBy]
	,ORDR.U_AppBy [ApprovedBy]
	,ORDR.U_NotedBy [NotedBy]
FROM ORDR
INNER JOIN OCRD ON OCRD.CardCode = ORDR.CardCode
WHERE ORDR.DocEntry = @DocEntry
