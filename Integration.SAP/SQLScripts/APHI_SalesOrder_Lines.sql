SELECT
	 ORDR.DocEntry
	,ORDR.DocNum
	,RDR1.LineNum
	,RDR1.ItemCode
	,OITM.ItemName
	,RDR1.WhsCode
	,OWHS.WhsName
	,RDR1.Quantity [TargetQty]
	,0 [Quantity]
	,RDR1.OpenQty [OpenQty]
	,ISNULL(RDR1.unitMsr, 'Manual') [UoMCode]
	,RDR1.NumPerMsr [UoMValue]
	,OUOM.UomName [UoMName]
FROM ORDR
INNER JOIN RDR1 ON RDR1.DocEntry = ORDR.DocEntry
INNER JOIN OITM ON RDR1.ItemCode = OITM.ItemCode
INNER JOIN OUOM ON RDR1.UomEntry = OUOM.UomEntry
INNER JOIN OWHS ON RDR1.WhsCode = OWHS.WhsCode
WHERE ORDR.DocEntry = @DocEntry