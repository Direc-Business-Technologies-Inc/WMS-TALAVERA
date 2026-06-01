SELECT T0.ItemCode
	,T0.Dscription [ItemDescription]
	,T0.unitMsr [UoMName]
	,T0.Quantity [Quantity]
	,T0.LineNum
FROM WTR1 AS T0
WHERE T0.DocEntry = @DocEntry