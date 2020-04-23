$cnt = 0
$failed = 0
while (1) {
                Write-Host "tried $cnt runs!!"

    $a = dotnet test --logger trx
    $cnt += 1

    if ($cnt % 10 -eq 0) {
        Write-Host "tried $cnt runs!!"
    }
    if(!($a[-2] -Match "Passed: 155")) {
        $failed += 1
        Write-Host "Failed $failed out of $cnt runs!!"
        Write-Host $a
    }
    Start-Sleep 1
}