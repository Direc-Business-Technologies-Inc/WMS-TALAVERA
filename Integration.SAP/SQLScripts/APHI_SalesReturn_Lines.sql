SELECT
	 ORDN.DocEntry
	,ORDN.DocNum
	,RDN1.LineNum
	,RDN1.ItemCode
	,OITM.ItemName
	,RDN1.WhsCode
	,OWHS.WhsName
	,0 [Quantity]
	,ISNULL(RDN1.unitMsr, 'Manual') [UoMCode]
	,RDN1.NumPerMsr [UoMValue]
	,OUOM.UomName [UoMName]
FROM ORDN
INNER JOIN RDN1 ON RDN1.DocEntry = ORDN.DocEntry
INNER JOIN OITM ON RDN1.ItemCode = OITM.ItemCode
INNER JOIN OUOM ON RDN1.UomEntry = OUOM.UomEntry
INNER JOIN OWHS ON RDN1.WhsCode = OWHS.WhsCode
WHERE ORDN.DocEntry = @DocEntry