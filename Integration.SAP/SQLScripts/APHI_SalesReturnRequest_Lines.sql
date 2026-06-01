SELECT
	 ORRR.DocEntry
	,ORRR.DocNum
	,RRR1.LineNum
	,RRR1.ItemCode
	,OITM.ItemName
	,RRR1.WhsCode
	,OWHS.WhsName
	,RRR1.Quantity [TargetQuantity]
	,0 [Quantity]
	,RRR1.OpenQty [OpenQuantity]
	,ISNULL(RRR1.unitMsr, 'Manual') [UoMCode]
	,RRR1.NumPerMsr [UoMValue]
	,OUOM.UomName [UoMName]
FROM ORRR
INNER JOIN RRR1 ON RRR1.DocEntry = ORRR.DocEntry
INNER JOIN OITM ON RRR1.ItemCode = OITM.ItemCode
INNER JOIN OUOM ON RRR1.UomEntry = OUOM.UomEntry
INNER JOIN OWHS ON RRR1.WhsCode = OWHS.WhsCode
WHERE ORRR.DocEntry = @DocEntry