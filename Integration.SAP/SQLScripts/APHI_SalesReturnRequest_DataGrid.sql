SELECT
	 ORRR.DocDate
	,ORRR.DocEntry
	,ORRR.DocNum
	,OCRD.CardCode
	,OCRD.CardName
	,ISNULL(ORRR.U_Remarks, '-') [Remarks]
FROM ORRR
INNER JOIN OCRD ON OCRD.CardCode = ORRR.CardCode