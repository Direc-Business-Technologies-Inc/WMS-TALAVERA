SELECT T0.DocEntry
    ,T0.DocNum
    ,T0.DocDate
    ,T0.Filler [FrmWhsCode]
    ,T1.WhsName [FrmWhsName]
    ,T0.ToWhsCode [ToWhsCode]
    ,T2.WhsName [ToWhsName]
    ,T0.U_SchlYear [SchlYearCode]
    ,SY.Name [SchlYearName]
    ,SY.U_YearFrom
    ,SY.U_YearTo
    ,ISNULL(T0.U_Remarks, '') [Remarks]
    ,T0.U_PrepBy [PreparedBy]
    ,T0.U_AppBy [ApprovedBy]
    ,T0.U_NotedBy [NotedBy]
    ,T0.U_TransferType [TransferTypeCode]
    ,ISNULL(
        (SELECT _T1.Descr
         FROM CUFD _T0
         INNER JOIN UFD1 _T1 
            ON _T0.TableID = _T1.TableID
            AND _T0.FieldID = _T1.FieldID
         WHERE 
            _T0.TableID = 'OWTQ' 
            AND _T0.FieldID = 31
            AND _T1.FldValue = T0.U_TransferType
        ), '') [TransferTypeName]
FROM OWTQ T0
LEFT JOIN OWHS T1 ON T0.Filler = T1.WhsCode
LEFT JOIN OWHS T2 ON T0.ToWhsCode = T2.WhsCode
LEFT JOIN [@SCHL_YR] SY ON SY.Code = T0.U_SchlYear
WHERE T0.DocEntry = @DocEntry