SELECT
     T0.DocEntry
    ,T0.DocNum
    ,T0.DocDate
    ,T0.DocDueDate
    ,T0.CardCode
    ,T0.CardName
    ,ISNULL(AX1.DocEntry, -1)  [GRPODocEntry]
    ,ISNULL(AX1.DocNum, -1)    [GRPODocNum]
    ,ISNULL(AX1.PODocEntry, -1)  [PODocEntry]
    ,ISNULL(AX1.PODocNum, -1)    [PODocNum]
    ,ISNULL(T0.Comments, '')  [Remarks]
    ,CASE 
        WHEN T0.CANCELED = 'Y' THEN 'Cancelled'	
        WHEN T0.DocStatus = 'C' THEN 'Closed'
        ELSE 'Open'
     END AS DocStatus
FROM OPRR T0
OUTER APPLY (
    SELECT TOP 1
         T3.DocEntry
        ,T3.DocNum
        ,T5.DocEntry AS PODocEntry
        ,T5.DocNum   AS PODocNum
    FROM PRR1 T1
    LEFT JOIN PDN1 T2 ON T2.DocEntry = T1.BaseEntry AND T2.ObjType = T1.BaseType
    LEFT JOIN OPDN T3 ON T3.DocEntry = T2.DocEntry
    LEFT JOIN POR1 T4 ON T4.DocEntry = T3.BaseEntry AND T4.ObjType = T2.BaseType
    LEFT JOIN OPOR T5 ON T5.DocEntry = T4.DocEntry
    WHERE T1.DocEntry = T0.DocEntry
) AX1
WHERE T0.DocStatus = 'O'