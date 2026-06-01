SELECT 
     T1.FldValue [Code]
    ,T1.Descr [Name]
FROM CUFD T0
INNER JOIN UFD1 T1 
    ON T0.TableID = T1.TableID 
    AND T0.FieldID = T1.FieldID
WHERE T0.TableID = 'ORPD' AND T0.FieldID = 14
ORDER BY T1.IndexID