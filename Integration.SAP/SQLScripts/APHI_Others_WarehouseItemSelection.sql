SELECT 
     T0.ItemCode
    ,T0.ItemName
    ,T0.OnHand [Quantity]
    ,ISNULL(T0.InvntryUom, 'Manual') [UoMCode]
    ,ISNULL(T2.UomName, 'Manual') [UoMName]
    ,ISNULL(T4.BaseQty, 1) [UoMValue]
FROM OITM T0
INNER JOIN OITB T1 ON T0.ItmsGrpCod = T1.ItmsGrpCod
LEFT JOIN OUOM T2 ON T0.InvntryUom = T2.UomCode
LEFT JOIN OUGP T3 ON T0.UgpEntry = T3.UgpEntry
LEFT JOIN UGP1 T4 ON T3.UgpEntry = T4.UgpEntry AND T4.UomEntry = T2.UomEntry
WHERE
    T0.SellItem = 'Y'
    AND T0.ItemType = 'I'
    AND T0.Canceled = 'N'
    AND T0.validFor = 'Y'