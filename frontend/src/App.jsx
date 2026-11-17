
import { useState, useEffect } from 'react'
import './App.css'

const STORAGE_KEY = 'paypulse-history'

// Payment status colors
const COLORS = {
  Approved: '#22c55e',
  Declined: '#ef4444',
  Rejected: '#f59e0b'
}

function App() {

  // =====================================
  // STATE
  // =====================================

  const [amount, setAmount] = useState('')
  const [payment, setPayment] = useState(null)

  const [history, setHistory] = useState(() => {
    try {
      const saved = JSON.parse(localStorage.getItem(STORAGE_KEY))
      return Array.isArray(saved) ? saved.slice(0, 10) : []
    } catch {
      return []
    }
  })

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [simulationActive, setSimulationActive] = useState(false)

  // Placeholder until Black Friday backend integration.
  const trafficBuckets = Array(12).fill(0)

  // =====================================
  // LOCAL STORAGE
  // =====================================

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(history))
  }, [history])

  // =====================================
  // PROCESS PAYMENT
  // =====================================

  async function processPayment(event) {
    event.preventDefault()

    const value = Number(amount)

    if (!Number.isFinite(value) || value <= 0) {
      setError('Please enter a valid amount.')
      return
    }

    setLoading(true)
    setError('')
    setPayment(null)

    try {
      const response = await fetch('/api/payments', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ amount: value })
      })

      if (!response.ok) {
        throw new Error('Unable to process payment.')
      }

      const result = await response.json()

      const transaction = {
        ...result,
        timestamp: new Date().toISOString()
      }

      setPayment(transaction)

      setHistory(previous => [
        transaction,
        ...previous
      ].slice(0, 10))

    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  // =====================================
  // STATISTICS
  // =====================================

  const total = history.length

  const approved = history.filter(
    item => item.status === 'Approved'
  ).length

  const declined = history.filter(
    item => item.status === 'Declined'
  ).length

  const rejected = history.filter(
    item => item.status === 'Rejected'
  ).length

  const percentage = value =>
    total > 0 ? (value / total) * 100 : 0

  // =====================================
  // NORMAL CHART DATA
  // =====================================

  const chartPayments = [...history].reverse()

  const maxAmount = Math.max(
    1,
    ...chartPayments.map(item => Number(item.amount) || 0)
  )

  // =====================================
  // CHART COMPONENT
  // =====================================

  function renderPaymentChart() {
    const chartWidth = 600
    const chartHeight = 220
    const bottom = 190
    const maxBarHeight = 150

    return (
      <div>
        <svg
          viewBox={`0 0 ${chartWidth} ${chartHeight}`}
          style={{
            width: '100%',
            height: '220px',
            display: 'block'
          }}
        >
          {/* Grid lines */}

          {[40, 90, 140, 190].map(y => (
            <line
              key={y}
              x1="0"
              y1={y}
              x2={chartWidth}
              y2={y}
              stroke="#e2e8f0"
              strokeWidth="1"
            />
          ))}

          {/* Payment bars */}

          {chartPayments.map((item, index) => {
            const paymentAmount = Number(item.amount) || 0

            const height = Math.max(
              15,
              (paymentAmount / maxAmount) * maxBarHeight
            )

            const x = 15 + index * 58
            const y = bottom - height

            const color = COLORS[item.status] || '#64748b'

            return (
              <g key={`${item.paymentId}-${index}`}>
                <rect
                  x={x}
                  y={y}
                  width="38"
                  height={height}
                  rx="4"
                  fill={color}
                >
                  <title>
                    €{item.amount} - {item.status}
                  </title>
                </rect>

                <text
                  x={x + 19}
                  y={Math.max(15, y - 8)}
                  textAnchor="middle"
                  fontSize="12"
                  fill="#475569"
                >
                  €{item.amount}
                </text>

                <text
                  x={x + 19}
                  y="208"
                  textAnchor="middle"
                  fontSize="11"
                  fill="#94a3b8"
                >
                  {index + 1}
                </text>
              </g>
            )
          })}
        </svg>

        <div className="activity-labels">
          <span>Oldest</span>
          <span>Individual Payments</span>
          <span>Newest</span>
        </div>

        <p className="chart-note">
          Showing {chartPayments.length} individual payments.
        </p>
      </div>
    )
  }

  // =====================================
  // BLACK FRIDAY CHART
  // =====================================

  function renderTrafficChart() {
    const maxTraffic = Math.max(1, ...trafficBuckets)

    return (
      <div>
        <svg
          viewBox="0 0 600 220"
          style={{
            width: '100%',
            height: '220px',
            display: 'block'
          }}
        >
          {[40, 90, 140, 190].map(y => (
            <line
              key={y}
              x1="0"
              y1={y}
              x2="600"
              y2={y}
              stroke="#e2e8f0"
              strokeWidth="1"
            />
          ))}

          {trafficBuckets.map((count, index) => {
            const height = count === 0
              ? 2
              : Math.max(10, (count / maxTraffic) * 150)

            return (
              <rect
                key={index}
                x={12 + index * 49}
                y={190 - height}
                width="32"
                height={height}
                rx="4"
                fill="#2563eb"
              >
                <title>{count} payments</title>
              </rect>
            )
          })}
        </svg>

        <div className="activity-labels">
          <span>0s</span>
          <span>30s</span>
          <span>60s</span>
        </div>

        <p className="chart-note">
          Waiting for traffic statistics...
        </p>
      </div>
    )
  }

  // =====================================
  // RENDER
  // =====================================

  return (
    <div className="app">

      {/* HEADER */}

      <header className="site-header">
        <div className="header-inner">

          <a className="brand" href="#overview">
            <span className="brand-mark">
              <span></span>
              <span></span>
            </span>
            <span>PayPulse</span>
          </a>

          <nav className="navigation">
            <a href="#overview" className="active">
              Overview
            </a>
            <a href="#payment">
              Payment Processing
            </a>
            <a href="#transactions">
              Transactions
            </a>
          </nav>

          <div className="header-avatar">PP</div>

        </div>
      </header>

      <main id="overview" className="dashboard">

        {/* PAGE HEADER */}

        <section className="page-heading">

          <div>
            <h1>Payment Processing</h1>
            <p>Simulate and process payments with PayPulse.</p>
          </div>

          <div className="heading-actions">

            <button
              className={`black-friday-button ${
                simulationActive ? 'active' : ''
              }`}
              onClick={() => setSimulationActive(true)}
            >
              Simulate Black Friday
            </button>

            {simulationActive && (
              <button
                className="stop-button"
                onClick={() => setSimulationActive(false)}
              >
                Stop
              </button>
            )}

          </div>
        </section>

        {/* PAYMENT FORM */}

        <section id="payment" className="payment-layout">

          <div className="panel payment-panel">

            <h2>Process a Payment</h2>

            <form onSubmit={processPayment}>

              <label htmlFor="amount">
                Amount (€)
              </label>

              <input
                id="amount"
                type="number"
                min="0.01"
                step="0.01"
                placeholder="Enter amount"
                value={amount}
                onChange={event => setAmount(event.target.value)}
              />

              <button
                className="primary-button"
                type="submit"
                disabled={loading}
              >
                {loading ? 'Processing...' : 'Process Payment'}
              </button>

            </form>

            {error && <p className="error">{error}</p>}

          </div>

          {/* PAYMENT RESULT */}

          <div className="panel result-panel">

            {payment ? (
              <>
                <span
                  className={`result-banner ${
                    payment.status.toLowerCase()
                  }`}
                >
                  {payment.status}
                </span>

                <div className="result-details">

                  <p>
                    <strong>Id</strong>
                    <span>{payment.paymentId}</span>
                  </p>

                  <p>
                    <strong>Amount</strong>
                    <span>€{payment.amount}</span>
                  </p>

                  <p>
                    <strong>Risk Score</strong>
                    <span>{payment.riskScore}/100</span>
                  </p>

                  <p>
                    <strong>Risk Engine</strong>
                    <span>{payment.riskPod}</span>
                  </p>

                  {payment.processorPod && (
                    <p>
                      <strong>Processor</strong>
                      <span>{payment.processorPod}</span>
                    </p>
                  )}

                </div>
              </>
            ) : (
              <div className="empty-result">
                <span className="empty-icon">✓</span>
                <h2>Ready to process</h2>
                <p>Submit a payment to see its result.</p>
              </div>
            )}

          </div>

        </section>

        {/* STATISTICS */}

        <section className="metrics-grid">

          <div className="metric">
            <span>Total Payments</span>
            <strong>{total}</strong>
          </div>

          <div className="metric">
            <span>Approved</span>
            <strong className="green">{approved}</strong>
          </div>

          <div className="metric">
            <span>Declined</span>
            <strong className="red">{declined}</strong>
          </div>

          <div className="metric">
            <span>Rejected</span>
            <strong>{rejected}</strong>
          </div>

        </section>

        {/* CHARTS */}

        <section className="charts-grid">

          {/* PAYMENT CHART */}

          <div className="panel chart-panel">

            <div className="panel-heading">

              <h2>
                {simulationActive
                  ? 'Payment Throughput'
                  : 'Payment Activity'}
              </h2>

              <span className="small-label">
                {simulationActive
                  ? 'Transactions per 5 seconds'
                  : 'Last 10 transactions'}
              </span>

            </div>

            {simulationActive
              ? renderTrafficChart()
              : renderPaymentChart()}

          </div>

          {/* PAYMENT OUTCOMES */}

          <div className="panel chart-panel">

            <h2>Payment Outcomes</h2>

            <div
              className="donut-chart"
              style={{
                background: total
                  ? `conic-gradient(
                      #16a34a 0 ${percentage(approved)}%,
                      #dc2626 ${percentage(approved)}%
                      ${percentage(approved + declined)}%,
                      #f59e0b ${percentage(approved + declined)}% 100%
                    )`
                  : '#e5e7eb'
              }}
            >
              <div className="donut-center">
                <strong>{total}</strong>
                <span>Total</span>
              </div>
            </div>

            <div className="legend">

              <p>
                <span>
                  <i className="dot green-dot"></i>
                  Approved
                </span>
                <strong>{approved}</strong>
              </p>

              <p>
                <span>
                  <i className="dot red-dot"></i>
                  Declined
                </span>
                <strong>{declined}</strong>
              </p>

              <p>
                <span>
                  <i className="dot orange-dot"></i>
                  Rejected
                </span>
                <strong>{rejected}</strong>
              </p>

            </div>

          </div>

        </section>

        {/* TRANSACTION HISTORY */}

        <section
          id="transactions"
          className="panel transactions-panel"
        >

          <div className="panel-heading">
            <h2>Recent Transactions</h2>
            <span className="small-label">
              Last 10 payments
            </span>
          </div>

          <div className="table-scroll">

            <table>
              <thead>
                <tr>
                  <th>Payment ID</th>
                  <th>Amount</th>
                  <th>Status</th>
                  <th>Risk Score</th>
                  <th>Time</th>
                </tr>
              </thead>

              <tbody>

                {history.length === 0 ? (
                  <tr>
                    <td colSpan="5" className="empty-table">
                      No transactions yet.
                    </td>
                  </tr>
                ) : (
                  history.map((item, index) => (
                    <tr key={`${item.paymentId}-${index}`}>

                      <td>{item.paymentId}</td>
                      <td>€{item.amount}</td>

                      <td>
                        <span
                          className={`status-badge ${
                            item.status.toLowerCase()
                          }`}
                        >
                          {item.status}
                        </span>
                      </td>

                      <td>{item.riskScore}</td>

                      <td>
                        {item.timestamp
                          ? new Date(item.timestamp).toLocaleTimeString([], {
                              hour: '2-digit',
                              minute: '2-digit'
                            })
                          : '—'}
                      </td>

                    </tr>
                  ))
                )}

              </tbody>
            </table>

          </div>

        </section>

        <footer>
          PayPulse · Payment Simulation Platform
        </footer>

      </main>
    </div>
  )
}

export default App
