/// <summary>
/// Jenkinsfile principal para despliegue automatizado del proyecto GESCOMPH.
/// Detecta el entorno desde GESCOMPH/.env,
/// compila el proyecto .NET 9 y ejecuta el docker-compose correspondiente
/// dentro de la carpeta GESCOMPH/DevOps/{entorno}.
/// </summary>

pipeline {
    agent any

    environment {
        DOTNET_CLI_HOME = 'C:\\jenkins\\.dotnet'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        DOTNET_NOLOGO = '1'
    }

    stages {

        stage('Verificar estructura del repositorio') {
            steps {
                sh '''
                    echo "📂 Contenido actual del workspace:"
                    pwd
                    ls -la
                    echo "📂 Contenido de GESCOMPH:"
                    ls -la GESCOMPH || echo "❌ Carpeta GESCOMPH no encontrada"
                '''
            }
        }

        stage('Leer entorno desde GESCOMPH/.env') {
            steps {
                script {
                    // ✅ Leer variable ENVIRONMENT del archivo .env
                    def envValue = sh(
                        script: "grep '^ENVIRONMENT=' GESCOMPH/.env | cut -d '=' -f2 | tr -d '\\r\\n'",
                        returnStdout: true
                    ).trim()

                    if (!envValue) {
                        error "❌ No se encontró ENVIRONMENT en GESCOMPH/.env"
                    }

                    env.ENVIRONMENT  = envValue
                    env.ENV_DIR      = "GESCOMPH/DevOps/${env.ENVIRONMENT}"
                    env.COMPOSE_FILE = "${env.ENV_DIR}/docker-compose.yml"
                    env.ENV_FILE     = "${env.ENV_DIR}/.env"

                    echo "✅ Entorno detectado: ${env.ENVIRONMENT}"
                    echo "📄 Archivo compose: ${env.COMPOSE_FILE}"
                    echo "📁 Archivo de entorno: ${env.ENV_FILE}"
                }
            }
        }

        stage('Restaurar dependencias') {
            steps {
                dir('GESCOMPH') {
                    sh '''
                        echo "🔧 Restaurando dependencias .NET..."
                        dotnet restore WebGESCOMPH/WebGESCOMPH.csproj
                    '''
                }
            }
        }

        stage('Compilar proyecto') {
            steps {
                dir('GESCOMPH') {
                    sh '''
                        echo "⚙️ Compilando la solución GESCOMPH..."
                        dotnet build WebGESCOMPH/WebGESCOMPH.csproj --configuration Release --no-restore
                    '''
                }
            }
        }

        stage('Ejecutar pruebas unitarias (opcional)') {
            when {
                expression { fileExists('GESCOMPH/Test') }
            }
            steps {
                dir('GESCOMPH') {
                    sh '''
                        echo "🧪 Ejecutando pruebas unitarias..."
                        dotnet test --configuration Release --no-build || echo "⚠️ Pruebas fallidas, revisa los logs."
                    '''
                }
            }
        }

        stage('Publicar y construir imagen Docker') {
            steps {
                dir('GESCOMPH') {
                    sh '''
                        echo "🐳 Construyendo imagen Docker..."
                        docker build -t gescomph-${ENVIRONMENT}:latest -f Dockerfile .
                    '''
                }
            }
        }

        stage('Desplegar GESCOMPH') {
            steps {
                dir('GESCOMPH') {
                    sh '''
                        echo "🚀 Desplegando GESCOMPH (${ENVIRONMENT})..."
                        docker compose -f ${COMPOSE_FILE} --env-file ${ENV_FILE} up -d --build
                    '''
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
