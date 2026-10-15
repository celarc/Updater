<?php
// update.php
// Lokacija: /public_html/update.php
// Datoteke: /updater/beta/
// Konfiguracija: /private/db_config_update.php  (izven document roota!)

error_reporting(E_ALL);
ini_set('display_errors', 0);          // na produkciji 0 - napake gredo v error_log
ini_set('display_startup_errors', 0);
ini_set('log_errors', 1);

// ---------------------------------------------------------------------------
// Osnovne poti
// ---------------------------------------------------------------------------
$BASE_DIR = realpath(__DIR__ . '/../../updater/');
if ($BASE_DIR === false || !is_dir($BASE_DIR)) {
    error_log('BMC update: update directory not found.');
    http_response_code(500);
    exit('Update directory not found.');
}

$CONFIG_FILE = realpath(__DIR__ . '/../../private/db_config_update.php');
if ($CONFIG_FILE === false || !is_file($CONFIG_FILE)) {
    error_log('BMC update: config file not found.');
    http_response_code(500);
    exit('Configuration error.');
}

$cfg = require $CONFIG_FILE;
if (!is_array($cfg)) {                 // require ob napaki vrne 1, ne array
    error_log('BMC update: config file did not return an array.');
    http_response_code(500);
    exit('Configuration error.');
}

// ---------------------------------------------------------------------------
// Pomozne funkcije
// ---------------------------------------------------------------------------
function requireAuth(): void
{
    header('WWW-Authenticate: Basic realm="BMC Update"');
    header('HTTP/1.1 401 Unauthorized');
    exit('Unauthorized');
}

/**
 * Preveri, ali je $target res znotraj $base (in ne v sosednji mapi
 * s podobnim imenom, npr. /updater_backup).
 */
function isInside(string $target, string $base): bool
{
    $base   = rtrim($base, DIRECTORY_SEPARATOR) . DIRECTORY_SEPARATOR;
    $target = rtrim($target, DIRECTORY_SEPARATOR) . DIRECTORY_SEPARATOR;
    return strpos($target, $base) === 0;
}

// ---------------------------------------------------------------------------
// BASIC AUTH - uporabnik in geslo iz baze bmcsi_param, tabela web_user
// ---------------------------------------------------------------------------

// Fallback za CGI/FastCGI, kjer Apache ne napolni PHP_AUTH_*
if (empty($_SERVER['PHP_AUTH_USER'])) {
    $authHeader = $_SERVER['HTTP_AUTHORIZATION']
        ?? $_SERVER['REDIRECT_HTTP_AUTHORIZATION']
        ?? '';
    if (stripos($authHeader, 'Basic ') === 0) {
        $decoded = base64_decode(substr($authHeader, 6), true);
        if ($decoded !== false && strpos($decoded, ':') !== false) {
            [$u, $p] = explode(':', $decoded, 2);
            $_SERVER['PHP_AUTH_USER'] = $u;
            $_SERVER['PHP_AUTH_PW']   = $p;
        }
    }
}

$user = $_SERVER['PHP_AUTH_USER'] ?? '';
$pass = $_SERVER['PHP_AUTH_PW']   ?? '';

if ($user === '' || $pass === '') {
    requireAuth();
}

try {
    $dsn = sprintf(
        'mysql:host=%s;port=%d;dbname=%s;charset=utf8mb4',
        $cfg['host'],
        $cfg['port'] ?? 3306,
        $cfg['name']
    );
    $pdo = new PDO($dsn, $cfg['user'], $cfg['pass'], [
        PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
        PDO::ATTR_EMULATE_PREPARES   => false,
    ]);
} catch (PDOException $e) {
    error_log('BMC update DB connect error: ' . $e->getMessage());
    http_response_code(500);
    exit('Database error.');
}

try {
    // Ce tabela web_user ima stolpec za onemogocanje racuna, dodaj:
    //   ... WHERE username = ? AND active = 1 LIMIT 1
    $stmt = $pdo->prepare('SELECT password_hash FROM web_users WHERE username = ? LIMIT 1');
    $stmt->execute([$user]);
    $row = $stmt->fetch();
} catch (PDOException $e) {
    error_log('BMC update DB query error: ' . $e->getMessage());
    http_response_code(500);
    exit('Database error.');
}

// Dummy hash, da je cas izvajanja enak tudi ce uporabnik ne obstaja.
$dummyHash = '$2y$10$abcdefghijklmnopqrstuv0123456789ABCDEFGHIJKLMNOPQRSTU';
$hash      = ($row !== false && isset($row['password_hash']))
    ? $row['password_hash']
    : $dummyHash;

$ok = password_verify($pass, $hash);

if ($row === false || !$ok) {
    error_log('BMC update: failed login for user "' . $user . '" from ' . ($_SERVER['REMOTE_ADDR'] ?? '?'));
    requireAuth();
}

// Od tu naprej je uporabnik avtenticiran.
// ---------------------------------------------------------------------------

$action = $_GET['action'] ?? '';

// ---------------------------------------------------------------------------
// action=list  -> seznam datotek s hashi
// ---------------------------------------------------------------------------
if ($action === 'list') {

    header('Content-Type: application/json; charset=utf-8');

    $path = str_replace('\\', '/', $_GET['path'] ?? '');
    $listDir = realpath($BASE_DIR . '/' . $path);

    if ($listDir === false || !is_dir($listDir) || !isInside($listDir, $BASE_DIR)) {
        http_response_code(404);
        exit(json_encode(['error' => 'Not found.']));
    }

    $files = [];

    $iterator = new RecursiveIteratorIterator(
        new RecursiveDirectoryIterator($listDir, FilesystemIterator::SKIP_DOTS)
    );

    foreach ($iterator as $fileInfo) {
        if (!$fileInfo->isFile()) {
            continue;
        }
        $fullPath     = $fileInfo->getPathname();
        $relativePath = substr($fullPath, strlen($listDir) + 1);
        $relativePath = str_replace('\\', '/', $relativePath);

        $files[] = [
            'path'        => $relativePath,
            'size'        => $fileInfo->getSize(),
            'modifiedUtc' => gmdate('Y-m-d\TH:i:s\Z', $fileInfo->getMTime()),
            'sha256'      => hash_file('sha256', $fullPath),
        ];
    }

    echo json_encode([
        'channel'      => 'beta',
        'generatedUtc' => gmdate('Y-m-d\TH:i:s\Z'),
        'files'        => $files,
    ], JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES);
    exit;
}

// ---------------------------------------------------------------------------
// action=file  -> prenos posamezne datoteke
// ---------------------------------------------------------------------------
if ($action === 'file') {

    $path = $_GET['path'] ?? '';
    if ($path === '') {
        http_response_code(400);
        exit('Missing path.');
    }

    $path   = str_replace('\\', '/', $path);
    $target = realpath($BASE_DIR . '/' . $path);

    // Zascita pred ../ napadi in pred sosednjimi mapami s podobnim imenom
    if ($target === false || !is_file($target) || !isInside(dirname($target), $BASE_DIR)) {
        http_response_code(404);
        exit('File not found.');
    }

    $size      = filesize($target);
    $useBase64 = (($_GET['enc'] ?? '') === 'base64');

    header('Cache-Control: no-store, no-cache, must-revalidate');
    header('Pragma: no-cache');

    if ($useBase64) {
        // enc=base64: vsebino vrnemo kot base64 besedilo (text/plain), da filter
        // vsebine na gostitelju ne blokira izvrsljivih datotek (.exe/.dll) z napako
        // 403 "body content-type denied". Nov updater tok sproti dekodira in
        // preveri SHA256 (hash je iz originalne datoteke, ne iz base64).
        header('Content-Type: text/plain; charset=us-ascii');
        // Dolzina base64 izhoda = ceil(size/3)*4 (vkljucno s paddingom na koncu).
        header('Content-Length: ' . (intdiv($size + 2, 3) * 4));

        // Pretocno kodiranje v blokih po veckratniku 3 bajtov, da vmesni bloki
        // nimajo paddinga (padding je dovoljen le na samem koncu). Tako ne
        // nalozimo cele datoteke v pomnilnik.
        $chunkBytes = 3 * 8192; // 24576 B -> 32768 base64 znakov
        $fh = fopen($target, 'rb');
        if ($fh === false) {
            http_response_code(500);
            exit('Read error.');
        }
        while (!feof($fh)) {
            $buf = fread($fh, $chunkBytes);
            if ($buf === false) {
                break;
            }
            echo base64_encode($buf);
        }
        fclose($fh);
        exit;
    }

    // Stari nacin (star updater brez enc=base64): surovi bajti, kot doslej.
    header('Content-Type: application/octet-stream');
    header('Content-Length: ' . $size);
    header('Content-Disposition: attachment; filename="' . basename($target) . '"');
    readfile($target);
    exit;
}

http_response_code(400);
exit('Invalid action.');