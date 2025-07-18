# Тест улучшенного поиска VB-Cable
# Это скрипт для проверки функции TestAudioDevices.TestDeviceEnumeration()

Add-Type -AssemblyName System.Reflection
Add-Type -AssemblyName System.Runtime

# Путь к DLL MORT_CORE
$mortCorePath = "DLL\MORT_CORE.dll"

try {
    # Загружаем сборку MORT_CORE
    $assembly = [System.Reflection.Assembly]::LoadFile("$(Get-Location)\$mortCorePath")
    
    # Получаем тип TestAudioDevices
    $testAudioDevicesType = $assembly.GetType("TestAudioDevices")
    
    if ($testAudioDevicesType) {
        Write-Host "Найден класс TestAudioDevices"
        
        # Получаем метод TestDeviceEnumeration
        $method = $testAudioDevicesType.GetMethod("TestDeviceEnumeration")
        
        if ($method) {
            Write-Host "Найден метод TestDeviceEnumeration"
            Write-Host "Вызов метода для тестирования поиска VB-Cable..."
            
            # Вызываем статический метод
            $result = $method.Invoke($null, $null)
            Write-Host "Результат: $result"
        } else {
            Write-Host "Метод TestDeviceEnumeration не найден"
        }
    } else {
        Write-Host "Класс TestAudioDevices не найден"
    }
    
} catch {
    Write-Host "Ошибка: $($_.Exception.Message)"
}
