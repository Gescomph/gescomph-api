pipeline {
    agent any

    environment {
        DOTNET_CLI_HOME = '/var/jenkins_home/.dotnet'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        DOTNET_NOLOGO = '1'
        PROJECT_PATH = 'GESCOMPH/WebGESCOMPH/WebGESCOMPH.csproj'
    }

    stages {

        // =========================
        // 1️⃣ CLONAR EL REPOSITORIO
        // =========================
        stage('Checkout código fuente') {
            steps {
                echo "📥 Clonando repositorio desde GitHub..."
                checkout scm

                // Verificar que los archivos existan en el workspace
                sh 'ls -R GESCOMPH/DevOps || true'
            }
        }

        // ======================================
        // 2️⃣ DETECTAR ENTORNO DESDE GESCOMPH/.env
        // ======================================
        stage('Detectar entorno desde GESCOMPH/.env') {
            steps {
                script {
                    def envValue = sh(
                        script: "grep '^ENVIRONMENT=' GESCOMPH/.env | cut -d '=' -f2 | tr -d '\\r\\n'",
                        returnStdout: true
                    ).trim()

                    if (!envValue) {
                        error "❌ No se encontró ENVIRONMENT en GESCOMPH/.env"
                    }

                    // Variables de entorno derivadas
                    env.ENVIRONMENT = envValue
                    env.ENV_DIR = "GESCOMPH/DevOps/${env.ENVIRONMENT}"
                    env.COMPOSE_FILE = "${env.ENV_DIR}/docker-compose.yml"
                    env.ENV_FILE = "${env.ENV_DIR}/.env"

                    echo """
                    ✅ Entorno detectado: ${env.ENVIRONMENT}
                    📄 Archivo compose: ${env.COMPOSE_FILE}
                    📁 Archivo de entorno: ${env.ENV_FILE}
                    """

                    // Validar existencia del archivo .env en el workspace
                    if (!fileExists(env.ENV_FILE)) {
                        error "❌ El archivo ${env.ENV_FILE} no existe en el workspace de Jenkins."
                    }
                }
            }
        }

        // =====================================
        // 3️⃣ COMPILAR Y PUBLICAR .NET (SDK)
        // =====================================
        stage('Compilar .NET dentro de contenedor SDK') {
            steps {
                script {
                    docker.image('mcr.microsoft.com/dotnet/sdk:9.0')
                        .inside('-v /var/run/docker.sock:/var/run/docker.sock -u root:root') {
                        sh '''
                            echo "🔧 Restaurando dependencias .NET..."
                            cd GESCOMPH
                            dotnet restore WebGESCOMPH/WebGESCOMPH.csproj
                            dotnet build WebGESCOMPH/WebGESCOMPH.csproj --configuration Release
                            dotnet publish WebGESCOMPH/WebGESCOMPH.csproj -c Release -o ./publish
                        '''
                    }
                }
            }
        }

        // ==========================
        // 4️⃣ CONSTRUIR IMAGEN DOCKER
        // ==========================
        stage('Construir imagen Docker') {
            steps {
                dir('GESCOMPH') {
                    sh """
                        echo "🐳 Construyendo imagen Docker para GESCOMPH (${env.ENVIRONMENT})"
                        docker build -t gescomph-${env.ENVIRONMENT}:latest -f WebGESCOMPH/Dockerfile .
                    """
                }
            }
        }

        // ==========================
        // 5️⃣ DESPLEGAR VIA DOCKER COMPOSE
        // ==========================
        stage('Desplegar GESCOMPH') {
            steps {
                dir('GESCOMPH') { // 👈 Ejecutar desde raíz de proyecto GESCOMPH
                    sh """
                        echo "🚀 Desplegando GESCOMPH para entorno: ${env.ENVIRONMENT}"
                        docker compose -f ${env.COMPOSE_FILE} --env-file ${env.ENV_FILE} up -d --build
                    """
                }
            }
        }
    }

    // ==========================
    // 🔁 POST-EJECUCIÓN
    // ==========================
    post {
        success {
            echo "🎉 Despliegue completado correctamente para ${env.ENVIRONMENT}"
        }
        failure {
            echo "💥 Error durante el despliegue en ${env.ENVIRONMENT}"
        }
        always {
            echo "🧹 Limpieza final del pipeline completada."
        }
    }
}
