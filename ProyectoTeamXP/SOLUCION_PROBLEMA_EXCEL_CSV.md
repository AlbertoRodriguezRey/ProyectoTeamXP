# Solución al Problema de Importación de Excel/CSV

## Problema Identificado

El sistema no estaba leyendo correctamente los datos del archivo CSV/Excel debido a varios problemas:

1. **Formato de números decimales**: El CSV usa coma decimal (formato español: `70,2`) pero el parser esperaba punto decimal (formato inglés: `70.2`)
2. **Parsing de fechas**: Las fechas en formato español (`19 ene`) no se parseaban correctamente
3. **Detección de filas de datos**: El código usaba posiciones fijas que no coincidían con la estructura real del CSV

## Cambios Realizados

### 1. Mejora del Parser de Números Decimales (`ParseDecimal`)

**Archivo**: `ProyectoTeamXP\Services\ExcelImportExportService.cs`

Se mejoró el método para detectar automáticamente el formato de número:

```csharp
- Detecta si la coma es separador decimal (español: 70,2)
- Detecta si la coma es separador de miles (inglés: 1,234.56)
- Maneja formato europeo con punto de miles y coma decimal (1.234,56)
- Convierte todo a formato invariante para parsing consistente
```

### 2. Mejora del Parser de Fechas (`ParseFecha`)

Se agregaron múltiples formatos de fecha soportados:

```csharp
- Fechas con nombre de mes en español: "19 ene", "1 feb"
- Fechas con formato numérico Excel (OADate)
- Fechas con formato dd/MM/yyyy
- Culturas español e inglés
```

### 3. Mejora de Detección de Filas de Seguimiento Diario (`ImportarSeguimientoDiario`)

**Cambios principales**:

- **Búsqueda dinámica de encabezados**: Busca la palabra "SEMANA" en el rango de filas 35-50 en lugar de usar posición fija
- **Lectura explícita de columnas**: Lee cada columna individualmente con su índice correcto
- **Filtrado mejorado**: Salta filas de "EJEMPLO", "RESUMEN" y "SEMANA Nº"
- **Prevención de duplicados**: Usa HashSet para evitar fechas duplicadas

### 4. Soporte para Archivos CSV

**Archivo**: `ProyectoTeamXP\Controllers\ExcelController.cs`

Se agregó funcionalidad para importar archivos CSV directamente:

- Método `ConvertirCsvAExcel`: Convierte CSV a formato Excel usando EPPlus
- Método `ParsearLineaCsv`: Parser de CSV que respeta comillas y comas
- Acepta extensiones `.csv` además de `.xlsx` y `.xls`

### 5. Actualización de la Interfaz

**Archivo**: `ProyectoTeamXP\Views\Excel\Index.cshtml`

- Actualizada para indicar soporte de archivos CSV
- Acepta formatos `.xlsx`, `.xls` y `.csv`

## Estructura del CSV Esperada

El sistema ahora lee correctamente el CSV con esta estructura:

```
Fila 4: NOMBRE Y EDAD | Valor del nombre
Fila 8: PESO INICIAL | Valor del peso (con coma decimal)
Fila 10: FECHA DE INICIO | Fecha de inicio
Fila 14: PASOS | Objetivo de pasos
Fila 16: CARDIO | Objetivo de cardio
Fila ~39-43: Encabezados del seguimiento diario
Fila ~44+: Datos de seguimiento (después de la fila EJEMPLO)
```

### Columnas del Seguimiento Diario:

1. SEMANA (A)
2. FECHA (B) - formatos: "19 ene", "1 feb", etc.
3. PESO EN AYUNAS (C) - formato: "70,2", "71,5"
4. HORA DEL PESAJE (D) - formato: "7:00", "7:15"
5. PROTEINA (E)
6. GRASA (F)
7. CARBS (G)
8. TOTAL CALORÍAS (H)
9. ENTRENO (I) - texto: "Día 1", "Día 2"
10. RENDIMIENTO DE LA SESIÓN (J) - texto: "BUENO", "NORMAL", "BRUTAL"
11. PASOS (K) - números: 10432, 9520
12. CARDIO (L)
13. DURACIÓN SUEÑO EN HORAS (M) - formato: "7:00", "8:00"
14. CALIDAD SUEÑO (N) - texto: "Alto", "Medio", "Bajo"
15. APETITO (O)
16. NIVEL DE ESTRÉS (P)
17. NOTAS EXTRA (Q)

## Cómo Usar

1. **Preparar el archivo**: Asegúrate de que tu archivo CSV/Excel siga la estructura esperada
2. **Acceder a la página**: Navega a `/Excel/Index`
3. **Seleccionar archivo**: Elige tu archivo `.xlsx`, `.xls` o `.csv`
4. **Importar**: Haz clic en "Importar Archivo"
5. **Verificar resultados**: El sistema mostrará un mensaje de éxito o error detallado

## Validaciones y Manejo de Errores

El sistema ahora:

- ? Valida que el archivo tenga datos
- ? Salta filas de ejemplo o resumen automáticamente
- ? Previene duplicados por fecha
- ? Maneja valores nulos en campos opcionales
- ? Muestra mensajes de error descriptivos
- ? No importa filas sin fecha válida

## Problemas Conocidos Resueltos

1. ? **Peso con coma decimal**: Ahora se parsea correctamente `70,2` ? `70.2`
2. ? **Fechas en español**: Ahora reconoce `19 ene`, `1 feb`, etc.
3. ? **Filas de ejemplo**: Se saltan automáticamente
4. ? **Posiciones dinámicas**: Busca encabezados en lugar de usar posiciones fijas
5. ? **Soporte CSV**: Ahora acepta archivos CSV directamente

## Recomendaciones

- **Usar el CSV de ejemplo**: El archivo `ALBERTO FASE 1 - DATOS.csv` ahora se importa correctamente
- **Verificar formato**: Asegúrate de que las fechas tengan formato de día y mes
- **Revisar logs**: Si hay errores, el mensaje indicará exactamente qué falló
- **Exportar plantilla**: Usa la función de exportar para obtener un formato correcto

## Pruebas Realizadas

- ? Importación del archivo CSV de ejemplo
- ? Parsing de números con coma decimal
- ? Parsing de fechas en español
- ? Detección correcta de encabezados
- ? Filtrado de filas de ejemplo
- ? Prevención de duplicados

---

**Fecha de actualización**: 26/02/2026  
**Versión**: 1.0
