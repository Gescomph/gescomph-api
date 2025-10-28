/// <summary>
/// Jenkinsfile principal para despliegue automatizado del proyecto GESCOMPH.
/// Este pipeline detecta el entorno desde GESCOMPH/.env,
/// compila el proyecto .NET 9 y ejecuta el docker-compose correspondiente dentro de la carpeta GESCOMPH/DevOps/{entorno}.
/// </summary>

pipeline {
    agent any

    environment {
        DOTNET_CLI_HOME = 'C:\\jenkins\\.dotnet'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        DOTNET_NOLOGO = '1'
    }

    stages {

        stage('Leer entorno desde GESCOMPH/.env') {
            steps {
                script {
                    // ✅ Leemos ENVIRONMENT desde el archivo .env
                    def envValue = powershell(
                        script: "(Get-Content 'GESCOMPH/.env' | Where-Object { \$_ -match '^ENVIRONMENT=' }) -replace '^ENVIRONMENT=', ''",
                        returnStdout: true
                    ).trim()

                    if (!envValue) {
                        error "❌ No se encontró ENVIRONMENT en GESCOMPH/.env"
                    }

                    env.ENVIRONMENT = envValue
                    env.ENV_DIR = "GESCOMPH/DevOps/${env.ENVIRONMENT}"
                    env.COMPOSE_FILE = "${env.ENV_DIR}/docker-compose.yml"
                    env.ENV_FILE = "${env.ENV_DIR}/.env"

                    echo "✅ Entorno detectado: ${env.ENVIRONMENT}"
                    echo "📄 Archivo compose: ${env.COMPOSE_FILE}"
                    echo "📁 Archivo de entorno: ${env.ENV_FILE}"
                }
            }
        }

        stage('Restaurar dependencias') {
            steps {
                dir('GESCOMPH') {
                    bat '''
                        echo 🔧 Restaurando dependencias .NET...
                        if not exist "C:\\jenkins\\dotnet" mkdir "C:\\jenkins\\dotnet"
                        dotnet restore WebGESCOMPH\\WebGESCOMPH.csproj
                    '''
                }
            }
        }

        stage('Compilar proyecto') {
            steps {
                dir('GESCOMPH') {
                    echo '⚙️ Compilando la solución GESCOMPH...'
                    bat 'dotnet build WebGESCOMPH\\WebGESCOMPH.csproj --configuration Release'
                }
            }
        }

        stage('Publicar y construir imagen Docker') {
            steps {
                dir('GESCOMPH') {
                    echo "🐳 Construyendo imagen Docker para GESCOMPH (${env.ENVIRONMENT})"
                    bat "docker build -t gescomph-${env.ENVIRONMENT}:latest -f Dockerfile ."
                }
            }
        }

        stage('Desplegar GESCOMPH') {
            steps {
                dir('GESCOMPH') {
                    echo "🚀 Desplegando GESCOMPH para entorno: ${env.ENVIRONMENT}"
                    bat "docker compose -f ${env.COMPOSE_FILE} --env-file ${env.ENV_FILE} up -d --build"
                }
            }
        }
    }

    post {
        success {
            echo "🎉 Despliegue completado correctamente para ${env.ENVIRONMENT}"
        }
        failure {
            echo "💥 Error durante el despliegue en ${env.ENVIRONMENT}"
        }
    }
}
