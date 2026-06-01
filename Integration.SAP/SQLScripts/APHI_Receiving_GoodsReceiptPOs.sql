SELECT 
	 T0.DocEntry
	,T0.DocNum
	,T0.DocDate
	,T0.DocDueDate
	,T0.CardCode
	,T0.U_RecBy [ReceivedBy]
	,AX1.*
	,AX2.*
FROM OPDN AS T0
OUTER APPLY (
	SELECT TOP 1  
		 ISNULL(T3.DocEntry, -1) [BaseEntry]
		,ISNULL(T3.DocNum, -1) [BaseDocNum]
		,T3.DocDate [PoDocDate]
		,T5.CardName
		,T6.Name [SupplierContactPerson]
		,T1.WhsCode
		,T4.WhsName
	FROM PDN1 T1
	INNER JOIN OPOR AS T3 ON T1.BaseEntry = T3.DocEntry
	INNER JOIN OWHS AS T4 ON T1.WhsCode = T4.WhsCode
	INNER JOIN OCRD AS T5 ON T0.CardCode = T5.CardCode
	LEFT JOIN OCPR AS T6 ON T3.CntctCode = T6.CntctCode
	WHERE T0.DocEntry = T1.DocEntry
) AS AX1
OUTER APPLY (
    SELECT
		 STRING_AGG(X1.ItemName, ', ') AS ItemDesc
    FROM PDN1 AS X0
    INNER JOIN OITM AS X1 ON X0.ItemCode = X1.ItemCode
    WHERE X0.DocEntry = T0.DocEntry
) AS AX2
WHERE 
	T0.Comments LIKE '%WMS%'