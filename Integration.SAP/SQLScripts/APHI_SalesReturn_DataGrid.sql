SELECT
	 ORDN.DocDate
	,ORDN.DocEntry
	,ORDN.DocNum
	,OCRD.CardCode
	,OCRD.CardName
	,ISNULL(ORDN.U_Remarks, '-') [Remarks]
FROM ORDN
INNER JOIN OCRD ON OCRD.CardCode = ORDN.CardCode
WHERE ORDN.Comments LIKE '%WMS%'