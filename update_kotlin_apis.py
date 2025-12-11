import os
import re

# Caminho do projeto Kotlin
kotlin_path = "../starkaidautomacao/app/src/main/java/com/starkaid/starkaidapp"

print("Atualizando chamadas de API no projeto Kotlin...")

# Padrões de substituição
replacements = [
    (r'@GET\("api/', r'@GET("api/v1/'),
    (r'@POST\("api/', r'@POST("api/v1/'),
    (r'@PUT\("api/', r'@PUT("api/v1/'),
    (r'@DELETE\("api/', r'@DELETE("api/v1/'),
    (r'@PATCH\("api/', r'@PATCH("api/v1/'),
    (r'"/api/', r'"/api/v1/'),
    (r'"api/', r'"api/v1/'),
    (r'https://starkaid\.runasp\.net/api/', r'https://starkaid.runasp.net/api/v1/'),
    (r'wss://starkaid\.runasp\.net/api/', r'wss://starkaid.runasp.net/api/v1/'),
]

# Excluir alguns endpoints que não devem ser versionados (webhooks, etc)
exclude_patterns = [
    'stripe-webhook',
    'wpp/',  # WppConnect pode ter rotas especiais
]

def should_exclude(line):
    for pattern in exclude_patterns:
        if pattern in line:
            return True
    return False

# Processar todos os arquivos .kt
for root, dirs, files in os.walk(kotlin_path):
    for file_name in files:
        if file_name.endswith(".kt"):
            file_path = os.path.join(root, file_name)
            
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            updated = False
            new_lines = []
            
            for line in lines:
                original_line = line
                
                # Verificar se deve excluir esta linha
                if should_exclude(line):
                    new_lines.append(line)
                    continue
                
                # Aplicar substituições
                for pattern, replacement in replacements:
                    # Verificar se já tem v1 ou v{version}
                    if 'v1/' not in line and 'v{version}' not in line:
                        line = re.sub(pattern, replacement, line)
                
                if line != original_line:
                    updated = True
                
                new_lines.append(line)
            
            if updated:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.writelines(new_lines)
                print(f"  ✓ Atualizado: {file_name}")

print("\nTodas as chamadas de API no Kotlin foram atualizadas!")

