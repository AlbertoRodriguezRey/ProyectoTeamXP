# Guía de Uso - Importación de Excel/CSV

## ?? Resumen Rápido

El sistema ahora soporta la importación correcta de archivos Excel y CSV con formato español (coma decimal, fechas en español).

## ? Cambios Principales

### 1. Soporte de Números con Coma Decimal
- **Antes**: `70,2` no se reconocía ?
- **Ahora**: `70,2` ? `70.2` ?

### 2. Soporte de Fechas en Español
- **Antes**: `19 ene` no se reconocía ?
- **Ahora**: `19 ene`, `1 feb`, `15/2/2026` todos funcionan ?

### 3. Soporte de Archivos CSV
- **Antes**: Solo `.xlsx` y `.xls` ?
- **Ahora**: También `.csv` ?

### 4. Detección Dinámica
- **Antes**: Posiciones fijas, fallos si cambia formato ?
- **Ahora**: Busca encabezados automáticamente ?

## ?? Instrucciones de Uso

### Paso 1: Preparar el Archivo

Tu archivo debe tener esta estructura:

```
INFORMACIÓN MIEMBRO DEL TEAM
?? NOMBRE Y EDAD: [Nombre del cliente]
?? OBJETIVOS: [Descripción]
?? PESO INICIAL: [Peso]kg
?? FECHA DE INICIO: [dd/mm/yyyy]
?? PASOS: [Objetivo diario]
?? CARDIO: [Descripción]

MEDIDAS
?? INICIO | SEMANA 2 | SEMANA 4 | ...
?? [Fecha] | [Fecha] | [Fecha]
?? PECHO: [valor]cm
?? BRAZO: [valor]cm
?? ...

SEGUIMIENTO DIARIO
?? Tabla con columnas: SEMANA, FECHA, PESO, etc.
```

### Paso 2: Acceder al Sistema

1. Inicia sesión en el sistema
2. Ve a `/Excel/Index` o busca "Gestión de Excel" en el menú
3. Verás dos opciones:
   - **Importar Excel/CSV**
   - **Ver Clientes**

### Paso 3: Importar el Archivo

1. Haz clic en "Seleccionar archivo"
2. Elige tu archivo (`.xlsx`, `.xls` o `.csv`)
3. Haz clic en "Importar Archivo"
4. Espera el mensaje de confirmación

### Paso 4: Verificar los Datos

Después de la importación:

1. Serás redirigido a la página de detalles del cliente
2. Verifica que los datos se hayan importado correctamente:
   - ? Perfil del cliente
   - ? Macros (día entreno y descanso)
   - ? Medidas corporales
   - ? Seguimiento diario

## ?? Formato de Datos Esperado

### Números con Coma Decimal
```
? Correcto: 70,2 | 71,5 | 96,3
? También: 70.2 | 71.5 | 96.3
? Incorrecto: 70'2 | 71-5
```

### Fechas
```
? Correcto: 19 ene | 1 feb | 15/2/2026
? También: 1/2/2026 | 19 enero 2026
? Incorrecto: 19-01 | enero 2026
```

### Pesos y Medidas
```
? Correcto: 70,2kg | 96cm | 32cm
? También: 70,2 | 96 | 32
? Incorrecto: 70.2 kg (con espacio en CSV)
```

### Horas
```
? Correcto: 7:00 | 7:15 | 8:30
? También: 7:0 | 7:5
? Incorrecto: 7h00 | 7.00
```

## ?? Solución de Problemas

### Problema: "No se encontró el nombre del cliente"
**Solución**: 
- Verifica que la celda con "NOMBRE Y EDAD" tenga un valor a la derecha
- Asegúrate de que no esté vacía

### Problema: "Error al importar seguimiento diario"
**Solución**:
- Verifica que las fechas estén en formato correcto
- Asegúrate de que la fila con "SEMANA" como encabezado exista
- Elimina filas con "EJEMPLO" o datos de prueba

### Problema: "Medidas no se importan"
**Solución**:
- Verifica que exista una fila con "MEDIDAS" como título
- Asegúrate de que haya una columna con "INICIO"
- Verifica que las fechas de las medidas sean válidas

### Problema: "Los números no se leen correctamente"
**Solución**:
- Usa coma decimal (`,`) o punto decimal (`.`)
- No uses separadores de miles en números pequeños
- Elimina espacios antes o después del número

## ?? Consejos y Mejores Prácticas

### ? Hacer

- Usa el CSV de ejemplo como plantilla
- Mantén el formato de las columnas
- Usa fechas consistentes
- Elimina filas de ejemplo antes de importar (o nómbralas "EJEMPLO")

### ? Evitar

- Cambiar el orden de las columnas
- Dejar celdas con fórmulas de Excel (#REF!, #DIV/0!)
- Usar caracteres especiales en las fechas
- Duplicar fechas en el seguimiento diario

## ?? Ejemplo de Datos Válidos

```csv
SEMANA,FECHA,PESO EN AYUNAS,HORA DEL PESAJE,...
1,19 ene,70,2,7:05,...
1,20 ene,70,3,7:00,...
1,21 ene,70,4,7:20,...
```

## ?? Actualización de Datos

Si importas un archivo para un usuario que ya tiene datos:

- ? Los datos antiguos se **reemplazan** completamente
- ? Se eliminan medidas y seguimientos anteriores
- ? Se actualizan los macros y perfil

**?? Importante**: Esta operación no se puede deshacer. Si quieres conservar datos antiguos, exporta primero el Excel actual.

## ?? Exportar Datos

Para obtener una plantilla o backup:

1. Ve a "Ver Clientes"
2. Haz clic en "Exportar Excel" para el cliente deseado
3. Se descargará un archivo `.xlsx` con todos los datos actuales

Este archivo puede ser:
- Usado como plantilla para nuevos clientes
- Modificado y re-importado
- Guardado como backup

## ?? Soporte

Si encuentras problemas:

1. Verifica los logs de error en la pantalla
2. Asegúrate de que el formato coincide con el ejemplo
3. Intenta exportar primero un Excel existente para ver el formato correcto
4. Revisa la documentación en `SOLUCION_PROBLEMA_EXCEL_CSV.md`

---

**Última actualización**: 26/02/2026  
**Versión del documento**: 1.0
