@echo off
powershell -NoProfile -Command "Add-Type -AssemblyName System.Device; $w=New-Object System.Device.Location.GeoCoordinateWatcher('High'); $w.Start(); Start-Sleep -Seconds 3; $p=$w.Position; Write-Host ('Latitud: ' + $p.Location.Latitude); Write-Host ('Longitud: ' + $p.Location.Longitude); Write-Host ('Precision: ' + $p.Location.HorizontalAccuracy + ' m'); Write-Host ('Estado: ' + $w.Status)"
pause
