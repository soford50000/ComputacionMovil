# Ejecutar con pwsh -File Tests/VerificarRecompensas.ps1 desde la raíz.
# Compila el cálculo de producción con un DTO mínimo: no requiere ejecutar Unity.
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$modelo = @'
public enum Rareza { Comun, PocoComun, Rara, Epica, Legendaria }
public class PezData {
    public Rareza rareza;
    public float fuerzaPez, pesoMinimo, pesoMaximo, longitudMinima, longitudMaxima;
}
'@
Add-Type -TypeDefinition ('using System;' + [Environment]::NewLine + $modelo + [Environment]::NewLine +
    (Get-Content Assets/Scripts/Captura/RecompensaCaptura.cs -Raw).Replace('using System;', ''))

function Exigir($condicion, $mensaje) { if (-not $condicion) { throw $mensaje } }
function DebeFallar([scriptblock]$accion) {
    $fallo = $false
    try { & $accion | Out-Null } catch { $fallo = $true }
    Exigir $fallo 'Se aceptó una entrada inválida.'
}

$pesos = @(50, 25, 15, 8, 2)
$totalProbabilidad = 0
$ingresoPonderado = 0.0
$filas = foreach ($archivo in Get-ChildItem Assets/Data/Peces/*.asset) {
    $datos = Get-Content $archivo.FullName -Raw
    $pez = [PezData]::new()
    foreach ($campo in @('rareza', 'fuerzaPez', 'pesoMinimo', 'pesoMaximo', 'longitudMinima', 'longitudMaxima')) {
        $valor = [regex]::Match($datos, "(?m)^  ${campo}: (.+)$").Groups[1].Value.Trim()
        $pez.$campo = [float]::Parse($valor, [Globalization.CultureInfo]::InvariantCulture)
    }
    $min = [RecompensaCaptura]::Calcular($pez, $pez.pesoMinimo, $pez.longitudMinima)
    $max = [RecompensaCaptura]::Calcular($pez, $pez.pesoMaximo, $pez.longitudMaxima)
    $medio = [RecompensaCaptura]::Calcular($pez, ($pez.pesoMinimo + $pez.pesoMaximo)/2, ($pez.longitudMinima + $pez.longitudMaxima)/2)
    Exigir ($min -gt 0 -and $max -gt $min) 'La especie no tiene variación positiva.'
    $suma = 0.0
    for ($i = 0; $i -le 100; $i++) {
        $peso = [float]($pez.pesoMinimo + ($pez.pesoMaximo - $pez.pesoMinimo)*$i/100)
        $anterior = 0
        for ($j = 0; $j -le 100; $j++) {
            $largo = [float]($pez.longitudMinima + ($pez.longitudMaxima - $pez.longitudMinima)*$j/100)
            $oro = [RecompensaCaptura]::Calcular($pez, $peso, $largo)
            Exigir ($oro -is [int] -and $oro -ge $min -and $oro -le $max -and $oro -ge $anterior) 'Monedas fuera de límites o caída al aumentar longitud.'
            if ($i -gt 0) {
                $pesoAnterior = [float]($pez.pesoMinimo + ($pez.pesoMaximo - $pez.pesoMinimo)*($i-1)/100)
                Exigir ($oro -ge [RecompensaCaptura]::Calcular($pez, $pesoAnterior, $largo)) 'Caída al aumentar peso.'
            }
            $anterior = $oro
            $suma += $oro
        }
    }
    $probabilidad = $pesos[[int]$pez.rareza]
    $totalProbabilidad += $probabilidad
    $ingresoPonderado += ($suma / 10201) * $probabilidad
    $nombre = [regex]::Match($datos, "(?m)^  nombreEspecie: '(.+)'\r?$").Groups[1].Value
    "| $nombre | $min | $medio | $max |"
}
Exigir ($filas.Count -eq 25) 'Cambió el catálogo: actualizar el análisis de balance.'
DebeFallar { [RecompensaCaptura]::Calcular($null, 1, 1) }
DebeFallar { [RecompensaCaptura]::Calcular($pez, [float]::NaN, 1) }
DebeFallar { [RecompensaCaptura]::Calcular($pez, $pez.pesoMaximo + 1, $pez.longitudMinima) }
$pez.pesoMinimo = 1; $pez.pesoMaximo = 1
$pez.longitudMinima = 2; $pez.longitudMaxima = 2
$pez.rareza = [Rareza]::Comun; $pez.fuerzaPez = 1
Exigir ([RecompensaCaptura]::Calcular($pez, 1, 2) -eq 40) 'Las medidas fijas no son neutrales.'
$pez.fuerzaPez = [float]::PositiveInfinity
DebeFallar { [RecompensaCaptura]::Calcular($pez, 1, 2) }
$pez.fuerzaPez = 1; $pez.pesoMaximo = 0
DebeFallar { [RecompensaCaptura]::Calcular($pez, 1, 2) }
$filas
"OK: 255025 ejemplares; enteros, límites, monotonía y entradas inválidas."
"Media ponderada (todos los intentos exitosos, sin cebo): $([Math]::Round($ingresoPonderado/$totalProbabilidad, 2))"
